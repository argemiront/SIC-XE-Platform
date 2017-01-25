using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Threading;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.IO;


namespace Simulador
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private enum Base
        {
            ASCII,
            BIN,
            DEC,
            HEX
        }

        private Montador montador;
        private Memoria memoria;
        private ULA ula;
        private LinkLoader linkLoader;
        private ControladorES dispIO;

        private List<Label> vetorMem;
        private List<Label> vetorEnd;
        private List<Label> vetorASCII;

        private Thread simThread;

        Base baseRegs;
        int offset;
        int tempoPasso;
        bool sicCompativel;
        bool modoExecContinuo;
        bool isRunning, programaCarregado, isPaused;

        #region Código para Janela Transparente
        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int cxLeftWidth;      // width of left border that retains its size
            public int cxRightWidth;     // width of right border that retains its size
            public int cyTopHeight;      // height of top border that retains its size
            public int cyBottomHeight;   // height of bottom border that retains its size
        };

        [DllImport("DwmApi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS pMarInset);

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            System.OperatingSystem OS = System.Environment.OSVersion;

            if (OS.Version.Major >= 6)
            {
                WindowInteropHelper windowInteropHelper = new WindowInteropHelper(this);
                IntPtr myHwnd = windowInteropHelper.Handle;
                HwndSource mainWindowSrc = System.Windows.Interop.HwndSource.FromHwnd(myHwnd);

                mainWindowSrc.CompositionTarget.BackgroundColor = Color.FromArgb(0, 0, 0, 0);

                MARGINS margins = new MARGINS()
                {
                    cxLeftWidth = 1,
                    cxRightWidth = 1,
                    cyBottomHeight = 1,
                    cyTopHeight = 82
                };

                DwmExtendFrameIntoClientArea(myHwnd, ref margins);
            }
            else
            {
                MessageBox.Show("Esse programa é recomendado para utilização no Windows Vista ou superior com o Aero habilitado.", "Simulador SIC/XE");
                grid0.Background = Brushes.White;
            }
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            memoria = new Memoria();
            dispIO = new ControladorES("texto padrão00");
            linkLoader = new LinkLoader(ref memoria);
            ula = new ULA(ref memoria, ref dispIO);

            vetorMem = new List<Label>();
            vetorEnd = new List<Label>();
            vetorASCII = new List<Label>();

            baseRegs = Base.HEX;
            modoExecContinuo = false;
        }

        private void LoadMemoria()
        {
            vetorASCII.Clear();
            vetorEnd.Clear();
            vetorMem.Clear();

            memoria.PosModif -= DelegateAtualizaMem;

            for (int i = 0; i <= 18; i++)
            {
                for (int j = 0; j <= 7; j++)
                {
                    var obj = this.FindName("l" + i.ToString() + j.ToString()) as System.Windows.Controls.Label;
                    vetorMem.Add(obj);
                }
            }

            for (int i = 0; i < vetorMem.Count; i++)
            {
                vetorMem[i].Content = null;
                vetorMem[i].Content = MakeHexa(memoria.Read(offset + i, 1), 2);
            }

            int l = 0;

            for (int i = 0; i <= 18; i++)
            {
                var obj = this.FindName("l" + i.ToString() + "E") as System.Windows.Controls.Label;
                obj.Content = MakeHexa(offset + i * 8, 6);
                vetorEnd.Add(obj);

                var objASCII = this.FindName("l" + i.ToString() + "A") as System.Windows.Controls.Label;
                objASCII.Content = null;

                for ( ; l < (i + 1) * 8; l++)
			    {
                    var ch = Byte.Parse(vetorMem[l].Content.ToString(), System.Globalization.NumberStyles.AllowHexSpecifier);
                    ch = (ch == 10 || ch == 9) ? (byte)32 : ch;
                    objASCII.Content += Convert.ToChar(ch).ToString();
			    }

                vetorASCII.Add(objASCII);
            }

            memoria.PosModif += DelegateAtualizaMem;
        }

        private string MakeASCII(byte[] valor)
        {
            string texto = "";

            for (int i = 0; i < valor.Length; i++)
            {
                var ch = (valor[i] == 10 || valor[i] == 9) ? 32 : valor[i];
                texto += Convert.ToChar(ch).ToString();
            }

            return texto;
        }

        private string MakeASCII(long valor, int tam)
        {
            string texto = "";

            for (int i = 0; i < tam; i++)
            {
                //Pega os 8 primeiros bits e converte
                byte ch = (byte)(valor & 0xFF);
                texto = Convert.ToChar(ch).ToString() + texto;
                valor = valor >> 8;
            }

            return texto;
        }

        private string MakeHexa(long valor, int tam)
        {
            string zero = "00000000000000000000";
            var texto = String.Format("{0:X}", valor);
            texto = (texto.Length < tam) ? (zero.Substring(0, tam - texto.Length) + texto) : texto;

            return texto;
        }

        //Função delegada que recebe a posição de memória que foi carregada e a quantidade
        //de posições para verificar se a tela deverá ser atualizada ou não
        private void DelegateAtualizaMem(int pos, int qtde)
        {
            //Verifico se a posição alterada está sendo mostrada na tela
            if (pos >= offset && pos < offset + 152)  
            {
                for (int i = 0; i < qtde; i++)
                {
                    if (pos + i < offset + 152)
                    {
                        var valorMem = memoria.Read(pos + i, 1);
                        this.Dispatcher.Invoke((ThreadStart)delegate() { vetorMem[pos - offset + i].Content = null; });
                        this.Dispatcher.Invoke((ThreadStart)delegate() { vetorMem[pos - offset + i].Content = MakeHexa(valorMem, 2); });
                    }
                    else
                        break;
                }

                //Atualizo a linha
                int nrLinha = (pos - offset) / 8;
                byte[] linha = new byte[8];

                for (int i = 0; i < 8; i++)
			    {
                    Label temp = null;
                    this.Dispatcher.Invoke((ThreadStart)delegate() { temp = this.FindName("l" + nrLinha.ToString() + i.ToString()) as Label; });
                    string tempstr = "";
                    this.Dispatcher.Invoke((ThreadStart)delegate() { tempstr = temp.Content.ToString(); });
                    linha[i] = Byte.Parse(tempstr, System.Globalization.NumberStyles.AllowHexSpecifier);
			    }

                this.Dispatcher.Invoke((ThreadStart)delegate() { vetorASCII[nrLinha].Content = null; });
                this.Dispatcher.Invoke((ThreadStart)delegate() { vetorASCII[nrLinha].Content = MakeASCII(linha); });
            }
            else if (pos + qtde >= offset && pos + qtde < offset + 152)
            {
                int qtdeReal = pos + qtde - offset;

                for (int i = 0; i < qtdeReal; i++)
                {
                    var valorMem = memoria.Read(offset + i, 1);
                    this.Dispatcher.Invoke((ThreadStart)delegate() { vetorMem[i].Content = null; });
                    this.Dispatcher.Invoke((ThreadStart)delegate() { vetorMem[i].Content = MakeHexa(valorMem, 2); });
                }

                //Atualizo a linha
                int nrLinha = 0;
                byte[] linha = new byte[8];

                for (int i = 0; i < 8; i++)
                {
                    Label temp = null;
                    this.Dispatcher.Invoke((ThreadStart)delegate() { temp = this.FindName("l" + nrLinha.ToString() + i.ToString()) as Label; });
                    string tempstr = "";
                    this.Dispatcher.Invoke((ThreadStart)delegate() { tempstr = temp.Content.ToString(); });
                    linha[i] = Byte.Parse(tempstr, System.Globalization.NumberStyles.AllowHexSpecifier);
                }

                this.Dispatcher.Invoke((ThreadStart)delegate() { vetorASCII[nrLinha].Content = null; });
                this.Dispatcher.Invoke((ThreadStart)delegate() { vetorASCII[nrLinha].Content = MakeASCII(linha); });
            }
        }

        //Converte o texto digitado em número e ajusta a posição da memória
        private void txbOffset_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Int32.TryParse(txbOffset.Text, System.Globalization.NumberStyles.AllowHexSpecifier, null, out offset))
            {
                try
                {
                    for (int i = 0; i < vetorMem.Count; i++)
                    {
                        vetorMem[i].Content = MakeHexa(memoria.Read(offset + i, 1), 2);
                    }

                    for (int i = 0; i < vetorEnd.Count; i++)
                    {
                        vetorEnd[i].Content = MakeHexa(offset + i * 8, 6);
                    }

                    for (int i = 0; i < vetorASCII.Count; i++)
                    {
                        byte[] linha = new byte[8];

                        for (int j = 0; j < 8; j++)
                        {
                            linha[j] = byte.Parse(vetorMem[i * 8 + j].Content.ToString(), System.Globalization.NumberStyles.AllowHexSpecifier);
                        }

                        vetorASCII[i].Content = MakeASCII(linha);
                    }
                }
                catch(Exception ex)
                {
                }
            }
        }

        private void txbTempoPasso_TextChanged(object sender, TextChangedEventArgs e)
        {
            try { Int32.TryParse(txbTempoPasso.Text, out tempoPasso); }
            catch (Exception ex) { }
        }

        private void BaseRegChanged(object sender, RoutedEventArgs e)
        {
            var opcao = sender as RadioButton;

            if (opcao.Content.ToString() == "ASCII")
                baseRegs = Base.ASCII;
            else if (opcao.Content.ToString() == "DEC")
                baseRegs = Base.DEC;
            else if (opcao.Content.ToString() == "HEX")
                baseRegs = Base.HEX;
            try
            {
                AtualizaRegs();
            }
            catch (Exception ex)
            {
            }
        }

        private void AtualizaRegs()
        {            
            switch (baseRegs)
            {
                case Base.ASCII:

                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbA.Text = MakeASCII(ula.registradores[ula.A], 3); });
                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbX.Text = MakeASCII(ula.registradores[ula.X], 3); });
                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbL.Text = MakeASCII(ula.registradores[ula.L], 3); });
                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbB.Text = MakeASCII(ula.registradores[ula.B], 3); });
                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbS.Text = MakeASCII(ula.registradores[ula.S], 3); });
                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbT.Text = MakeASCII(ula.registradores[ula.T], 3); });
                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbF.Text = MakeASCII(ula.registradores[ula.F], 5); });
                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbPC.Text = MakeASCII(ula.registradores[ula.PC], 3); });
                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbSW.Text = MakeASCII(ula.registradores[ula.SW], 3); });
                    break;

                case Base.DEC:
                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbA.Text = ula.registradores[ula.A].ToString(); });
                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbX.Text = ula.registradores[ula.X].ToString(); });
                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbL.Text = ula.registradores[ula.L].ToString(); });
                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbB.Text = ula.registradores[ula.B].ToString(); });
                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbS.Text = ula.registradores[ula.S].ToString(); });
                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbT.Text = ula.registradores[ula.T].ToString(); });
                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbF.Text = ula.registradores[ula.F].ToString(); });
                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbPC.Text = ula.registradores[ula.PC].ToString(); });
                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbSW.Text = ula.registradores[ula.SW].ToString(); });
                    break;

                case Base.HEX:
                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbA.Text = MakeHexa(ula.registradores[ula.A], 6); });
                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbX.Text = MakeHexa(ula.registradores[ula.X], 6); });
                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbL.Text = MakeHexa(ula.registradores[ula.L], 6); });
                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbB.Text = MakeHexa(ula.registradores[ula.B], 6); });
                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbS.Text = MakeHexa(ula.registradores[ula.S], 6); });
                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbT.Text = MakeHexa(ula.registradores[ula.T], 6); });
                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbF.Text = MakeHexa(ula.registradores[ula.F], 10); });
                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbPC.Text = MakeHexa(ula.registradores[ula.PC], 6); });
                    this.Dispatcher.Invoke((ThreadStart)delegate() { txbSW.Text = MakeHexa(ula.registradores[ula.SW], 6); });
                    break;

                default:
                    break;
            }
        }

        private void AtualizaInst()
        {
            this.Dispatcher.Invoke((ThreadStart)delegate() { txbInstLoaded.Text = Convert.ToString(ula.instrucao.Codigo, 2); });
            this.Dispatcher.Invoke((ThreadStart)delegate() { txbInstrucao.Text = Convert.ToString(ula.instrucao.Codigo, 16).ToUpper(); });
            this.Dispatcher.Invoke((ThreadStart)delegate() { txbFN.Text = (ula.flags.N)? "1" : "0";});
            this.Dispatcher.Invoke((ThreadStart)delegate() { txbFI.Text = (ula.flags.I)? "1" : "0";});
            this.Dispatcher.Invoke((ThreadStart)delegate() { txbFX.Text = (ula.flags.X)? "1" : "0";});
            this.Dispatcher.Invoke((ThreadStart)delegate() { txbFB.Text = (ula.flags.B)? "1" : "0";});
            this.Dispatcher.Invoke((ThreadStart)delegate() { txbFP.Text = (ula.flags.P)? "1" : "0";});
            this.Dispatcher.Invoke((ThreadStart)delegate() { txbFE.Text = (ula.flags.E)? "1" : "0";});
        }

        private void AtualizaExecs()
        {
            try
            {
                this.Dispatcher.Invoke((ThreadStart)delegate() { txbInstrucao_Copy.Text = ula.instrucao.ToString(); });
                this.Dispatcher.Invoke((ThreadStart)delegate() { txbParamLoaded.Text = Convert.ToString(ula.parametroCarregado, 2); });
            }
            catch (Exception ex)
            {
            }
        }

        private void AtualizaComentarios()
        {
            this.Dispatcher.Invoke((ThreadStart)delegate() { lblInstrucao.Content = ula.instrucao.ToString(); });
            this.Dispatcher.Invoke((ThreadStart)delegate() { lblTamanho.Content = ula.Tamanho.ToString(); });
            this.Dispatcher.Invoke((ThreadStart)delegate() { lblModoEnd.Content = ula.enderecamento; });
            this.Dispatcher.Invoke((ThreadStart)delegate() { lblComent.Content = ""; });
        }

        private void menuSair_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MenuMontarPP_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.CheckFileExists = true;
            fileDialog.CheckPathExists = true;
            fileDialog.Filter = @"Arquivo Fonte(*.asm)|*.asm|Todos os arquivos (*.*)|*.*";
            fileDialog.Title = @"Abrir arquivo para execução";
            bool? result = fileDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {

                try
                {
                    montador = new Montador();
                    montador.SICCompativel = sicCompativel;
                    montador.MontarArquivo(fileDialog.FileName);

                    //reset na memória
                    memoria.Dealloc(0, -1);

                    //Carrega o novo programa
                    linkLoader.ResetMemoria();
                    linkLoader.QtdePosMem = 0;
                    ula.registradores[ula.PC] = linkLoader.Load(montador.NomeProgBin);

                    AtualizaRegs();
                    LoadMemoria();

                    programaCarregado = true;
                }
                catch (Exception ex)
                {
                    string msg = "Erro na montagem. Detalhes:\n" + ex.Message;
                    MessageBox.Show(msg, "Erro na carga do programa", MessageBoxButton.OK, MessageBoxImage.Error);
                    programaCarregado = false;
                }
            }
        }

        private void menuLoadIO_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.CheckFileExists = true;
            fileDialog.CheckPathExists = true;
            fileDialog.Filter = @"Arquivo de Texto(*.txt)|*.txt|Todos os arquivos (*.*)|*.*";
            fileDialog.Title = @"Abrir arquivo de registro de I/O";
            bool? result = fileDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                TextReader disp = new StreamReader(fileDialog.FileName);
                string texto = disp.ReadToEnd();
                disp.Close();

                dispIO.SetTexto(texto);
            }
        }

        private void menuAbrir_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.CheckFileExists = true;
            fileDialog.CheckPathExists = true;
            fileDialog.Filter = @"Arquivo Objeto(*.sic)|*.sic|Todos os arquivos (*.*)|*.*";
            fileDialog.Title = @"Abrir arquivo para execução";
            bool? result = fileDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                //reset na memória
                memoria.Dealloc(0, -1);

                //Carrega o novo programa
                linkLoader.ResetMemoria();
                linkLoader.QtdePosMem = 0;
                ula.registradores[ula.PC] = linkLoader.Load(fileDialog.FileName);

                AtualizaRegs();
                LoadMemoria();

                programaCarregado = true;
            }
            else
                programaCarregado = false;
        }

        private void menuLoadDispIO_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.CheckFileExists = true;
            fileDialog.CheckPathExists = true;
            fileDialog.Filter = @"Arquivo de Texto(*.txt)|*.txt|Todos os arquivos (*.*)|*.*";
            fileDialog.Title = @"Registrar dispositivos de entrada e saída";
            bool? result = fileDialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                TextReader disp = new StreamReader(fileDialog.FileName);
                string texto = disp.ReadToEnd();
                disp.Close();

                MatchCollection matches = Regex.Matches(texto, @"\S\S");

                foreach (var item in matches)
                {
                    byte io;
                    if (Byte.TryParse(item.ToString(), System.Globalization.NumberStyles.AllowHexSpecifier, null, out io))
                        dispIO.RegistreIO(io);
                }

                MessageBox.Show("Dispositivos carregados com sucesso!", "Carregador de dispositivos", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ModoOperacao(object sender, RoutedEventArgs e)
        {
            if (((MenuItem)sender).Header.ToString() == "Contínuo")
            {
                this.Dispatcher.Invoke((ThreadStart)delegate() { menuContinuo.IsChecked = true; });
                modoExecContinuo = true;
                this.Dispatcher.Invoke((ThreadStart)delegate() { txbTempoPasso.IsEnabled = true; });
                this.Dispatcher.Invoke((ThreadStart)delegate() { menuPassoAPasso.IsChecked = false; });
            }
            else
            {
                this.Dispatcher.Invoke((ThreadStart)delegate() { menuPassoAPasso.IsChecked = true; });
                modoExecContinuo = false;
                this.Dispatcher.Invoke((ThreadStart)delegate() { menuContinuo.IsChecked = false; });
                this.Dispatcher.Invoke((ThreadStart)delegate() { txbTempoPasso.IsEnabled = false; });
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            sicCompativel = ((MenuItem)sender).IsChecked;
        }

        private void Play(object sender, RoutedEventArgs e)
        {
            simThread = new Thread(Execute);
            simThread.Start();
        }

        private void Pause(object sender, RoutedEventArgs e)
        {
            if (isRunning)
            {
                if (simThread.ThreadState == ThreadState.WaitSleepJoin ||
                    simThread.ThreadState == ThreadState.Running ||
                    simThread.ThreadState == ThreadState.Background)
                {
                    simThread.Suspend();
                    MessageBox.Show("Simulação Pausada!", "Simulador SIC/XE", MessageBoxButton.OK, MessageBoxImage.Information);
                    isPaused = true;
                }
                else if (simThread.ThreadState == ThreadState.Suspended)
                {
                    simThread.Resume();
                    MessageBox.Show("Simulação rodando!", "Simulador SIC/XE", MessageBoxButton.OK, MessageBoxImage.Information);
                    isPaused = false;
                }
            }
        }

        private void Stop(object sender, RoutedEventArgs e)
        {
            if (isRunning)
            {
                try { simThread.Suspend(); }
                catch (Exception ex) { }

                var result = MessageBox.Show(
                    "Você gostaria de cancelar a simulação?",
                    "Simulador SIC/XE",
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.OK)
                {
                    try
                    {
                        if (simThread.ThreadState == ThreadState.Suspended)
                            simThread.Resume();

                        simThread.Abort();
                        MessageBox.Show("Simulação Cancelada!", "Simulador SIC/XE", MessageBoxButton.OK, MessageBoxImage.Information);
                        isRunning = isPaused = programaCarregado = false;
                    }
                    catch (Exception ex)
                    {
                        var result2 = MessageBox.Show("Erro no cancelamento!\nVocê gostaria de ver detalhes?", "Simulador SIC/XE", MessageBoxButton.OKCancel, MessageBoxImage.Error);

                        if (result2 == MessageBoxResult.OK)
                            MessageBox.Show(ex.Message, "Simulador SIC/XE", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                    simThread.Resume();
            }
        }

        private void Execute()
        {
            if (modoExecContinuo)
            {
                isRunning = true;

                while (true)
                {
                    if (!ula.Execute())
                        Thread.CurrentThread.Abort();

                    AtualizaRegs();
                    AtualizaInst();
                    AtualizaExecs();
                    AtualizaComentarios();

                    Thread.Sleep(tempoPasso);
                }
            }
            else
            {
                if (!ula.Execute())
                    Thread.CurrentThread.Abort();

                AtualizaRegs();
                AtualizaInst();
                AtualizaExecs();
                AtualizaComentarios();
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (simThread.ThreadState == ThreadState.Suspended || simThread.ThreadState == ThreadState.Stopped)
                    simThread.Resume();

                simThread.Abort();
            }
            catch (Exception ex)
            {
            }

            base.OnClosing(e);
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            Sobre sobreForm = new Sobre();

            sobreForm.ShowDialog();
        }

        private void menuAjuda_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Para detalhes de uso do software ver manual de utilização.", "Simulador SIC/XE");
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.P)
                Play(sender, new RoutedEventArgs());
        }

        private void menuExportar_Click(object sender, RoutedEventArgs e)
        {
            if (programaCarregado)
            {
                var fileDialog = new Microsoft.Win32.SaveFileDialog();
                fileDialog.AddExtension = true;
                fileDialog.CheckPathExists = true;
                fileDialog.DefaultExt = "mif";
                fileDialog.OverwritePrompt = true;
                fileDialog.Title = "Salvar arquivo de inicialização de memória da DE2";

                bool? result = fileDialog.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    TextWriter arquivo = new StreamWriter(fileDialog.FileName);
                    arquivo.WriteLine(Resource1.STR_MIF_COMMENT);
                    arquivo.WriteLine();

                    arquivo.WriteLine("WIDTH=8;");
                    arquivo.WriteLine("DEPTH=4096;");
                    arquivo.WriteLine();
                    arquivo.WriteLine("ADDRESS_RADIX=HEX;");
                    arquivo.WriteLine("DATA_RADIX=HEX;");
                    arquivo.WriteLine();
                    arquivo.WriteLine("CONTENT BEGIN");

                    for (int i = 0; i < linkLoader.QtdePosMem; i++)
                        arquivo.WriteLine('\t' + string.Format("{0:X}", i) + "  :   " + string.Format("{0:X}", memoria[i]) + ";");

                    arquivo.WriteLine("END;");
                    arquivo.Close();
                }
            }
            else
            {
                MessageBox.Show("Nenhum programa carregado. Carregue um programa executável.", "Nenhum programa carregado", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

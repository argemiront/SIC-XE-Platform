using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Simulador
{
    class LinkLoader
    {
        struct DadosProgsCarregados
        {
            public int pos;
            public int tam;
            public Dictionary<string, int> definicoes;

            public DadosProgsCarregados(Dictionary<string, int> def, int pos, int tam)
            {
                this.pos = pos;
                this.tam = tam;
                definicoes = def;
            }
        }

        public Memoria memoria;

        public int QtdePosMem { get; set; }

        //Armazena quais programas já estão carregados para evitar carga duplicada
        private Dictionary<string, DadosProgsCarregados> progsCarregados;
        private Dictionary<string, long> referencias;

        public LinkLoader(ref Memoria memoria)
        {
            this.memoria = memoria;

            progsCarregados = new Dictionary<string, DadosProgsCarregados>();
            referencias = new Dictionary<string, long>();
        }

        public int Load(string filePath)
        {
            //Zera a quantidade de posições de memória que foram carregadas
            //Esta informação é utilizada para a exportação da memória para a DE2

            //Abre o arquivo do programa
            System.IO.BinaryReader programa = new BinaryReader(File.OpenRead(filePath));
            int retorno = 0;

            //Salva o diretório do arquivo
            int index = filePath.LastIndexOf('\\');
            string dirPath = filePath.Remove(index + 1);

            try
            {
                //variáveis para verificação das secções
                byte pByte;

                string nomeProg = "";
                long iEndInic = 0;
                long iTam;

                //HEADER 
                pByte = programa.ReadByte();

                if (pByte == 0x48) //'H'
                {
                    //Faz a leitura dos bytes do cabeçalho
                    byte[] bNome = programa.ReadBytes(6);
                    byte[] bEndInic = programa.ReadBytes(3);
                    byte[] bTam = programa.ReadBytes(3);

                    //Faz as devidas conversões dos bytes
                    nomeProg = Word.ByteToString(bNome);
                    iEndInic = Word.ByteToInt(bEndInic);
                    iTam = Word.ByteToInt(bTam) + 1; //Adicionamos um byte que marca o fim do arquivo

                    //Retira o espaço no nome, se houver
                    nomeProg = nomeProg.Trim();

                    bool reloc = true;

                    //Se o endereço inicial é diferente de zero então o programa não é relocável
                    if (iEndInic != 0)
                        reloc = false;

                    //Faz o cast de long para uint do endereço inicial para solicitação de memória
                    uint endAloc = (uint)iEndInic;

                    //Se o programa já estiver carregado, retornamos a função
                    if (progsCarregados.ContainsKey(nomeProg))
                        retorno = progsCarregados[nomeProg].pos;

                    //Se não houver memória disponível para o programa, então um erro é acionado
                    //Se houver espaço e endAloc = 0 (relocável) então endAloc passará a ter o endereço
                    //inicial de alocação do programa
                    if (memoria.Alloc((int)iTam, ref endAloc, reloc))
                    {
                        progsCarregados.Add(nomeProg, new DadosProgsCarregados() {definicoes = new Dictionary<string,int>(), pos = (int)endAloc, tam = (int)iTam });
                        iEndInic = endAloc;
                    }
                    else
                    {
                        progsCarregados.Remove(nomeProg);
                        throw new OutOfMemoryException();
                    }

                    //Verifica fim de linha correta no HEADER
                    pByte = programa.ReadByte();
                    if (pByte != 10)
                    {
                        //TODO: programa no formato incorreto
                    }
                }
                else
                {
                    //TODO: exceção, arquivo executável em um formato incorreto
                }

                //DEFINITIONS
                pByte = (byte)programa.PeekChar();

                if (pByte == 0x44)
                {
                    int chTeste = 0;

                    do
                    {
                        if (programa.PeekChar() == 0x44) programa.ReadByte();
                        byte[] bNomeVar = programa.ReadBytes(6);
                        string nomeVar = Word.ByteToString(bNomeVar);
                        nomeVar = nomeVar.Trim();

                        byte[] bEndVar = programa.ReadBytes(3);
                        int endvar = (int)Word.ByteToInt(bEndVar);

                        progsCarregados[nomeProg].definicoes.Add(nomeVar, (int)(endvar + iEndInic));

                        //Se a linha não acabou ainda continuamos a ler a próxima variável
                        if (programa.PeekChar() != 0x0A)
                        {
                            chTeste = 0x44;
                            continue;
                        }

                        //Se chegamos no final da linha, lemos o '0A'
                        //para avançarmos para o próximo identificador
                        programa.ReadByte();
                        chTeste = programa.PeekChar();

                    } while (chTeste == 0x44); //Enquanto lermos uma linha de definições = 'D'
                }

                //REFERENCES
                pByte = (byte)programa.PeekChar();

                if (pByte == 0x52)
                {
                    programa.ReadByte();

                    int refTeste = 0;

                    do
                    {
                        byte[] bNomeLib = programa.ReadBytes(6);
                        string nomeLib = Word.ByteToString(bNomeLib);
                        nomeLib = nomeLib.Trim();

                        //Procura se é um programa e está carregado
                        DadosProgsCarregados progRef;

                        //verificamos se uma referência já está declarada. Se já estiver não fazemos nada
                        if (!referencias.ContainsKey(nomeLib))
                        {
                            //Se é um programa carregado adicionamos o endereço na definição
                            if (progsCarregados.TryGetValue(nomeLib, out progRef))
                            {
                                referencias.Add(nomeLib, progRef.pos);
                            }
                            //Se não é um programa, iremos procurar nas definições de programas
                            else
                            {
                                var varRef =
                                    from prog in progsCarregados
                                    where prog.Value.definicoes.ContainsKey(nomeLib)
                                    select prog.Value.definicoes[nomeLib];

                                /*
                                 * Se nenhuma definição é encontrada nos programas carregados
                                 * iremos procurar uma biblioteca que esteja no mesmo diretório
                                 * do programa carregado. Se for encontrada, deve ter sido encontrada
                                 * apenas 1 definição, se foi encontrada mais de uma há um erro.
                                 */
                                if (varRef.Count() > 0)
                                {
                                    //Um endereço encontrado. Sucesso!
                                    if (varRef.Count() == 1)
                                    {
                                        referencias.Add(nomeLib, varRef.First());
                                    }
                                    else
                                    {
                                        //TODO: ambiguidade na atribuição da biblioteca
                                        throw new ArgumentException("Ambiguidade na busca da biblioteca " + nomeLib);
                                    }
                                }
                                //Se nenhum endereço encontrado, tentar carregar o programa
                                else
                                {
                                    if (File.Exists(dirPath + "\\" + nomeLib + ".sic"))
                                    {
                                        int posLib = Load(dirPath + "\\" + nomeLib + ".sic");

                                        if (posLib != -1)
                                            referencias.Add(nomeLib, posLib);
                                        else
                                            throw new ArgumentException("Falha na abertura da biblioteca " + nomeLib);
                                    }
                                    //O arquivo pode não estar no diretório atual. Abre caixa de diálogo para busca
                                    else
                                    {
                                        string msg = "Biblioteca " + nomeLib + " não encontrada no diretório atual. Selecione o arquivo.";
                                        System.Windows.MessageBox.Show(msg, "Biblioteca não encontrada", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);

                                        var fileDialog = new Microsoft.Win32.OpenFileDialog();
                                        fileDialog.CheckFileExists = true;
                                        fileDialog.CheckPathExists = true;
                                        fileDialog.Filter = @"Arquivo Objeto SIC (*.sic)|*.sic";
                                        fileDialog.Title = @"Abrir biblioteca do SIC";
                                        bool? result = fileDialog.ShowDialog();

                                        if (result.HasValue && result.Value)
                                        {
                                            int posProg = Load(fileDialog.FileName);
                                            referencias.Add(nomeLib, posProg);
                                        }
                                        else
                                        {
                                            throw new ArgumentException("Falha na abertura da biblioteca " + nomeLib);
                                        }
                                    }
                                }
                            }
                        }

                        //Se a linha não acabou ainda continuamos a ler a próxima variável
                        if (programa.PeekChar() != 0x0A)
                        {
                            refTeste = 0x52;
                            continue;
                        }

                        //Se chegamos no final da linha, lemos o '0A'
                        //para avançarmos para o próximo identificador
                        programa.ReadByte();
                        refTeste = programa.PeekChar(); 

                    } while (refTeste == 0x52);
                }

                //TEXT
                pByte = (byte)programa.PeekChar();

                if (pByte == 0x54)
                {
                    int tTeste = 0;

                    do
                    {
                        //lemos o "T" e avançamos a leitura
                        programa.ReadByte();

                        byte[] bEnd = programa.ReadBytes(3);
                        long iEnd = Word.ByteToInt(bEnd);
                        int qtdeBytes = programa.ReadByte();
                        byte[] texto = programa.ReadBytes(qtdeBytes);

                        for (int i = 0; i < qtdeBytes; i++)
                        {
                            memoria.Write((int)(iEndInic + iEnd + i), 1, (long)texto[i]);
                            QtdePosMem++;
                        }

                        if (programa.PeekChar() != 0x0A)
                        {
                            //Erro no formato do arquivo
                        }
                        else
                            programa.ReadByte();

                        tTeste = programa.PeekChar();

                    } while (tTeste == 0x54);
                }
                else
                {
                    //TODO: exceção, arquivo executável em um formato incorreto
                }

                //MODIFICATIONS
                pByte = (byte)programa.PeekChar();

                if (pByte == 0x4D)
                {
                    int mTeste = 0;

                    do
                    {
                        //Lê o identificador da seção
                        programa.ReadByte();

                        //Lê o endereço inicial do registro de modificação
                        //e a quantidade de meio bytes
                        byte[] bEnd = programa.ReadBytes(3);
                        long iEnd = Word.ByteToInt(bEnd);
                        byte meioBytes = programa.ReadByte();

                        long valorSoma;

                        if (programa.PeekChar() == 0x0A)
                        {
                            //Programa SIC relocável

                            valorSoma = memoria.Read((int)(iEndInic + iEnd), 2);
                            
                            //Esse valor não deve ser alterado, pois, a ULA não reconheceria a indexação
                            //valorSoma = valorSoma & 0x7FFF; //retira o bit referente ao flag X

                            memoria.Write((int)(iEndInic + iEnd), 2, iEndInic + valorSoma);
                        }
                        else
                        {
                            //Programa SIC/XE relocável

                            byte sinal = programa.ReadByte();
                            byte[] bnome = programa.ReadBytes(6);
                            string nome = Word.ByteToString(bnome);
                            nome = nome.Trim();

                            //Busca o endereço da variável, que já deve estar carregado por refExt
                            if (!referencias.TryGetValue(nome, out valorSoma))
                            {
                                //TODO: erro no programa. Variável chamada sem ser referenciada
                                throw new ArgumentException("erro no programa. Variável chamada sem ser referenciada");
                            }

                            if (meioBytes == 3)
                            {
                                long valorMem = memoria.Read((int)(iEndInic + iEnd), 2);
                                valorMem = valorMem & 0xF000; //Isola os 4 bits de flags

                                memoria.Write((int)(iEndInic + iEnd), 2, valorMem + valorSoma);
                            }
                            else if (meioBytes == 5)
                            {
                                long valorMem = memoria.Read((int)(iEndInic + iEnd), 3);
                                valorMem = valorMem & 0xF00000; //Isola os 4 bits de flags

                                memoria.Write((int)(iEndInic + iEnd), 3, valorMem + valorSoma);
                            }
                            else if (meioBytes == 6)
                            {
                                long valorMem = memoria.Read((int)(iEndInic + iEnd), 3);

                                switch (sinal)
                                {
                                    case (int)'+':
                                        memoria.Write((int)(iEndInic + iEnd), 3, valorMem + valorSoma);
                                        break;

                                    case (int)'-':
                                        memoria.Write((int)(iEndInic + iEnd), 3, valorMem - valorSoma);
                                        break;

                                    case (int)'/':
                                        memoria.Write((int)(iEndInic + iEnd), 3, valorMem / valorSoma);
                                        break;

                                    case (int)'*':
                                        memoria.Write((int)(iEndInic + iEnd), 3, valorMem * valorSoma);
                                        break;

                                    default:
                                        //TODO: Erro no formato do arquivo.
                                        break;
                                }
                            }
                        }


                        if (programa.PeekChar() != 0x0A)
                        {
                            //TODO: Erro no formato do arquivo
                        }
                        else
                            programa.ReadByte();

                        mTeste = programa.PeekChar();

                    } while (mTeste == 0x4D);
                }

                //END
                pByte = (byte)programa.PeekChar();

                if (pByte == 0x45)
                {
                    programa.ReadByte(); //Lê o "E"

                    if (programa.PeekChar() == 0x0A)
                        if (retorno == 0)
                            return (int)iEndInic;
                    else
                    {
                        byte[] bEndInicPC = programa.ReadBytes(3);
                        long iEndInicPC = Word.ByteToInt(bEndInicPC);

                        if (retorno == 0)
                            return (int)(iEndInicPC + iEndInic);
                    }
                }
                else
                {
                    //TODO: implementar erro no formato do arquivo
                }
            }
            catch (Exception)
            {
                //TODO: implementar falha. Arquivo terminou abruptamente
                throw;
            }
            finally
            {
                //Fecha o arquivo
                programa.Close();
            }

            return retorno;
        }

        public void ResetMemoria()
        {
            progsCarregados.Clear();
            referencias.Clear();
        }
    }
}

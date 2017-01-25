using System;
using System.Collections.Generic;
using System.Windows;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Simulador
{
    class Montador
    {
        struct LinhaProg
        {
            public int LOCCTR;
            public string INST;
            public int nrLinha;
            public bool isINST;

            public LinhaProg(int _LOCCTR, string _INST, int _nrLinha, bool isINST)
            {
                LOCCTR = _LOCCTR;
                INST = _INST;
                nrLinha = _nrLinha;
                this.isINST = isINST;
            }
        }

        Dictionary<string, int> SYMTAB = new Dictionary<string, int>();
        Dictionary<string, int> EXTDEF = new Dictionary<string, int>();
        Dictionary<string, int> EXTREF = new Dictionary<string, int>();
        Dictionary<string, SetInstrucoes> setInstrucoes = new Dictionary<string, SetInstrucoes>();
        List<string> secoes = new List<string>();
        List<string> modificadores = new List<string>();
        Queue<LinhaProg> instToProcess = new Queue<LinhaProg>();
        List<int> fimSecao = new List<int>();
        int LOCCTR;
        string LinhaInstMont;
        bool BASE = false;
        bool sicCompativel = false;
        List<string> programaMontado = new List<string>();
        string[] programa;
        string dirPath;
        int reloc;

        public string NomeProgBin = "";

        /// <summary>
        /// Flag que indica se a montagem das instruções devem ser compatíveis com
        /// o padrão SIC Standard.
        /// "true" = compatível;
        /// "false" = não compatível.
        /// </summary>
        public bool SICCompativel
        {
            get { return sicCompativel; }
            set { sicCompativel = value; }
        }

        public Montador()
        {
            LoadInstrucoes();

            reloc = 0;
        }

        /// <summary>
        /// Monta arquvivos objeto de acordo com o endereço especificado pelo "filePath". O montador salva cria os seguintes arquivos:
        /// Nome_do_programa.sic : arquivo em formato objeto binário para execução na plataforma;
        /// Nome_do_programa_ASCII.sic : arquivo em formato objeto em modo ASCII para facilitar leitura em computador;
        /// Nome_do_programa Montado.asm : arquivo fonte original com os dados de montagem.
        /// 
        /// A função salva um grupo de arquivos especificados acima para seção de programa existente no arquivo original no mesmo diretório do mesmo.
        /// </summary>
        /// <param name="filePath">Endereço do arquivo fonte para ser montado.</param>
        public void MontarArquivo(string filePath)
        {
            //Salva o diretório do arquivo para usar depois
            int index = filePath.LastIndexOf('\\');
            dirPath = filePath.Remove(index + 1);

            //Prepara as tabelas
            ClearSYMTAB();
            fimSecao.Clear();
            secoes.Clear();

            //Variáveis locais de controle do algoritmo
            TextReader prog = new StreamReader(filePath);
            string textoProg = prog.ReadToEnd();
            prog.Close();
            LOCCTR = 0;
            reloc = 0;
            string nomeProg;
            int primeiraInst = 0;
            programaMontado.Clear();

            //Põe o texto do programa em linhas separadas
            programa = textoProg.Split('\n');

            //Verifica o nome do programa e se é relocável
            Regex regStart = new Regex(@"(?<nome>\w+)\s+START\s(?<valor>\w+)");
            nomeProg = regStart.Match(programa[0]).Groups["nome"].Value;
            reloc = Int32.Parse(regStart.Match(programa[0]).Groups["valor"].Value);
            LOCCTR = reloc;
            programaMontado.Add(programa[0]);

            //Armazena o nome do programa para execução conjunta com a montagem pelo simulador
            NomeProgBin = dirPath + nomeProg + ".sic";


            //---- Expressões Regulares para as linhas ----//
            //Verificamos que é uma instrução
            string strINST = @"\s*((?<label>\S+)?\s+(?<tamES>\+)?)?((?<inst>" + Resource1.STR_INSTRUCOES + @")|(?<dir>" + Resource1.STR_DIRETIVAS + @"))(\s+(?<param>(\S+\s*)*)?)?$";
            Regex regINST = new Regex(strINST);

            for (int i = 1; i < programa.Length; i++)
            {
                //Teste para montagem do programa analisado
                programaMontado.Add(programa[i]);

                //Pula os comentários
                if (programa[i].StartsWith("."))
                    continue;

                //Retira o \r do final
                programa[i] = programa[i].TrimEnd('\r');

                //Verifica se o formato geral está correto
                Match instMatch = regINST.Match(programa[i]);

                if (instMatch.Success)
                {
                    SetInstrucoes inst;

                    //Pesquisa label
                    if (instMatch.Groups["label"].Value != "" && instMatch.Groups["dir"].Value != "EQU" && instMatch.Groups["dir"].Value != "CSECT")
                    {
                        try
                        {
                            SYMTAB.Add(instMatch.Groups["label"].Value, LOCCTR);
                        }
                        catch (Exception ex)
                        {
                            //Trata o erro no programa
                        }
                    }

                    //Pesquisa Instrução ou diretiva para aumento do LOCCTR
                    int extendida = 0;

                    if (instMatch.Groups["tamES"].Value == "+")
                        extendida = 1;

                    if (setInstrucoes.TryGetValue(instMatch.Groups["inst"].Value, out inst))
                    {
                        instToProcess.Enqueue(new LinhaProg(LOCCTR, programa[i], i, true));

                        int svcSIC = 0;
                        if (sicCompativel)
                            svcSIC = (inst.Codigo == 0xB0) ? 1 : 0;

                        LOCCTR += inst.Tamanho + extendida + svcSIC; //controle do SVC no SIC
                    }
                    else
                    {
                        string LOCCTRHexa = "";
                        string endRegistro = "";
                        string zero = "000000";

                        switch (instMatch.Groups["dir"].Value)
                        {
                            case "WORD":

                                //Código para implementar o código das diretivas do WORD junto com a montagem.
                                //Não será implementado agora.
                                string paramWORD = instMatch.Groups["param"].Value.Substring(0, instMatch.Groups["param"].Value.Length);
                                Match paramMatchWORD = Regex.Match(paramWORD, @"(?<op1>\w+)\s*(?<op>(\+|-|\*|/))\s*(?<op2>\w+)");
                                int vlrCteWORD = 0;

                                if (paramMatchWORD.Success)
                                {
                                    /*
                                     * O valor da constante declarada por WORD, só é utilizado quando estamos lidando com declarações
                                     * em várias seções de uma variável externa.
                                     * 
                                     * se não estamos na primeira seção acrescentamos os registros de modificação. Caso contrário, calculamos
                                     * o valor da constante.
                                     */
                                    if (secoes.Count == 0)
                                    {
                                        int op1 = 0, op2 = 0;

                                        /*
                                         * Verificamos se estamos tratando de variáveis ou de constantes e lemos os valores.
                                         * Se não está na tabela de símbolos esperamos que seja um número inteiro. Se não for
                                         * nenhum dos dois temos um erro no programa
                                         */
                                        if (!SYMTAB.TryGetValue(paramMatchWORD.Groups["op1"].Value, out op1))
                                            if (!Int32.TryParse(paramMatchWORD.Groups["op1"].Value, out op1))
                                                throw new ArgumentException("Erro na montagem da linha " + i.ToString());

                                        if (!SYMTAB.TryGetValue(paramMatchWORD.Groups["op2"].Value, out op2))
                                            if (!Int32.TryParse(paramMatchWORD.Groups["op2"].Value, out op2))
                                                throw new ArgumentException("Erro na montagem da linha " + i.ToString());

                                        switch (paramMatchWORD.Groups["op"].Value)
                                        {
                                            case "+":
                                                vlrCteWORD = op1 + op2;
                                                break;

                                            case "-":
                                                vlrCteWORD = op1 - op2;
                                                break;

                                            case "*":
                                                vlrCteWORD = op1 * op2;
                                                break;

                                            case "/":
                                                vlrCteWORD = op1 / op2;
                                                break;

                                            default:
                                                //ERRO no Programa
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        //Registro de modificação 1
                                        var locctrHexaModif1 = String.Format("{0:X}", LOCCTR);
                                        locctrHexaModif1 = (locctrHexaModif1.Length < 6) ? (zero.Substring(0, 6 - locctrHexaModif1.Length) + locctrHexaModif1) : locctrHexaModif1.Substring(0, 6);
                                        modificadores.Add("M" + locctrHexaModif1 + "06" + "+" + paramMatchWORD.Groups["op1"].Value);

                                        //Registro de modificação 2
                                        var locctrHexaModif2 = String.Format("{0:X}", LOCCTR);
                                        locctrHexaModif2 = (locctrHexaModif2.Length < 6) ? (zero.Substring(0, 6 - locctrHexaModif2.Length) + locctrHexaModif2) : locctrHexaModif2.Substring(0, 6);
                                        modificadores.Add("M" + locctrHexaModif2 + "06" + paramMatchWORD.Groups["op"].Value + paramMatchWORD.Groups["op2"].Value);
                                    }                                    
                                }
                                else
                                {
                                    vlrCteWORD = Int32.Parse(instMatch.Groups["param"].Value);
                                }

                                //Montagem da posD. para arq. montado
                                LOCCTRHexa = String.Format("{0:X}", LOCCTR);
                                endRegistro = (LOCCTRHexa.Length < 4) ? (zero.Substring(0, 4 - LOCCTRHexa.Length) + LOCCTRHexa) : LOCCTRHexa;
                                var vlrCteWORDHexa = String.Format("{0:X}", vlrCteWORD);
                                var vlrCteWORDHexaM = (vlrCteWORDHexa.Length < 6) ? (zero.Substring(0, 6 - vlrCteWORDHexa.Length) + vlrCteWORDHexa) : vlrCteWORDHexa;
                                programaMontado[i] = endRegistro + "\t" + programa[i] + "\t\t" + vlrCteWORDHexaM;
                                //Fim da montagem

                                //Armazena o código para gravar no campo de texto
                                instToProcess.Enqueue(new LinhaProg(LOCCTR, vlrCteWORDHexaM, i, false));

                                //Incrementa LOCCTR
                                LOCCTR += 3;
                                break;

                            case "RESW":
                                //Montagem da posD. para arq. montado
                                LOCCTRHexa = String.Format("{0:X}", LOCCTR);
                                endRegistro = (LOCCTRHexa.Length < 4) ? (zero.Substring(0, 4 - LOCCTRHexa.Length) + LOCCTRHexa) : LOCCTRHexa;
                                programaMontado[i] = endRegistro + "\t" + programa[i];
                                //Fim da montagem

                                LOCCTR += 3 * Int32.Parse(instMatch.Groups["param"].Value);
                                break;

                            case "BYTE":    //"-3" usado para retirar as plicas e o "C" ou "X", que não são contabilizados
                                //Montagem da posD. para arq. montado
                                LOCCTRHexa = String.Format("{0:X}", LOCCTR);
                                endRegistro = (LOCCTRHexa.Length < 4) ? (zero.Substring(0, 4 - LOCCTRHexa.Length) + LOCCTRHexa) : LOCCTRHexa;

                                //Monta o código para armazenar no campo texto
                                string codParam = "";
                                string parametro = instMatch.Groups["param"].Value;

                                if (parametro.StartsWith("C")) //trata-se de um texto
                                {
                                    foreach (var item in parametro.Substring(2, parametro.Length - 3))
                                    {
                                        codParam += String.Format("{0:X}", Convert.ToInt32(item));
                                    }
                                }
                                else //Trata-se de um código em HEXA
                                {
                                    codParam = parametro.Substring(2, 2);
                                }

                                instToProcess.Enqueue(new LinhaProg(LOCCTR, codParam, i, false));

                                //Monta a linha correta
                                programaMontado[i] = endRegistro + "\t" + programa[i] + "\t\t" + codParam;
                                //Fim da montagem

                                //Incrementa o LOCCTR
                                if (parametro.StartsWith("C"))
                                    LOCCTR += instMatch.Groups["param"].Value.Length - 3;
                                else
                                    LOCCTR++;

                                break;

                            case "RESB":
                                //Montagem da posD. para arq. montado
                                LOCCTRHexa = String.Format("{0:X}", LOCCTR);
                                endRegistro = (LOCCTRHexa.Length < 4) ? (zero.Substring(0, 4 - LOCCTRHexa.Length) + LOCCTRHexa) : LOCCTRHexa;
                                programaMontado[i] = endRegistro + "\t" + programa[i];
                                //Fim da montagem

                                LOCCTR += Int32.Parse(instMatch.Groups["param"].Value);
                                break;

                            case "EQU":
                                //Montagem da posD. para arq. montado
                                LOCCTRHexa = String.Format("{0:X}", LOCCTR);
                                endRegistro = (LOCCTRHexa.Length < 4) ? (zero.Substring(0, 4 - LOCCTRHexa.Length) + LOCCTRHexa) : LOCCTRHexa;
                                //Fim da montagem
                                string parametroMont = "";

                                string paramEQU = instMatch.Groups["param"].Value.Substring(0, instMatch.Groups["param"].Value.Length);
                                Match paramMatchEQU = Regex.Match(paramEQU, @"(?<op1>\w+)\s*(?<op>(\+|-|\*|/))\s*(?<op2>\w+)");

                                if (paramEQU == "*")
                                {
                                    SYMTAB.Add(instMatch.Groups["label"].Value, LOCCTR);
                                    parametroMont = "1";
                                }
                                else if (paramMatchEQU.Success)
                                {
                                    int op1 = 0, op2 = 0;

                                    if (!SYMTAB.TryGetValue(paramMatchEQU.Groups["op1"].Value, out op1) &&
                                        !SYMTAB.TryGetValue(paramMatchEQU.Groups["op2"].Value, out op2))
                                    {
                                        op1 = op2 = 0;
                                        parametroMont = "0";
                                    }

                                    else
                                    {
                                        if (secoes.Count != 0)
                                            parametroMont = paramEQU;
                                        else
                                            parametroMont = "0";

                                        switch (paramMatchEQU.Groups["op"].Value)
                                        {
                                            case "+":
                                                SYMTAB.Add(instMatch.Groups["label"].Value, op1 + op2);
                                                break;

                                            case "-":
                                                SYMTAB.Add(instMatch.Groups["label"].Value, op1 - op2);
                                                break;

                                            case "*":
                                                SYMTAB.Add(instMatch.Groups["label"].Value, op1 * op2);
                                                break;

                                            case "/":
                                                SYMTAB.Add(instMatch.Groups["label"].Value, op1 / op2);
                                                break;

                                            default:
                                                //ERRO no Programa
                                                break;
                                        }
                                    }
                                }
                                else
                                {
                                    //ERRO no programa
                                }

                                //Montagem da posD. para arq. montado
                                var vlrCteEQUHexa = String.Format("{0:X}", parametroMont);
                                var vlrCteEQUHexaM = (vlrCteEQUHexa.Length < 6) ? (zero.Substring(0, 6 - vlrCteEQUHexa.Length) + vlrCteEQUHexa) : vlrCteEQUHexa;
                                programaMontado[i] = endRegistro + "\t" + programa[i] /*+ "\t\t" + vlrCteEQUHexaM - Não precisa ser montado */;
                                //Fim da montagem


                                LOCCTR++;
                                break;

                            case "EXTDEF":
                                //Adiciona a variável à tabela de definições externas para depois achar o valor
                                string paramEXTDEF = instMatch.Groups["param"].Value.Substring(0, instMatch.Groups["param"].Value.Length);
                                MatchCollection paramMatchEXTDEF = Regex.Matches(paramEXTDEF, @"(?<ops>\w+),?");

                                foreach (var match in paramMatchEXTDEF)
                                {
                                    Match temp = match as Match;
                                    EXTDEF.Add(temp.Groups["ops"].Value, 0);
                                }
                                break;

                            case "EXTREF":
                                string paramEXTREF = instMatch.Groups["param"].Value.Substring(0, instMatch.Groups["param"].Value.Length);
                                MatchCollection paramMatchEXTREF = Regex.Matches(paramEXTREF, @"(?<ops>\w+),?");

                                foreach (var match in paramMatchEXTREF)
                                {
                                    Match temp = match as Match;
                                    EXTREF.Add(temp.Groups["ops"].Value, 0);
                                }
                                break;

                            case "END":
                                string paramEND;

                                if (instMatch.Groups["param"].Value != "")
                                {
                                    paramEND = instMatch.Groups["param"].Value.Substring(0, instMatch.Groups["param"].Value.Length);
                                }
                                else
                                    paramEND = "0";

                                Match paramMatchEND = Regex.Match(paramEND, @"(?<valor>\d+)\s*");


                                try
                                {
                                    primeiraInst = SYMTAB[paramMatchEND.Groups["valor"].Value];
                                }
                                catch (Exception ex)
                                {
                                    //ERRO no programa
                                }
                                break;

                            case "CSECT":
                                //Salvo os dados da seção atual
                                MontarCabecalho(LOCCTR, reloc, nomeProg);
                                MontarDefinicoesExt();
                                MontarReferenciasExt();
                                MontarInstrucoes();
                                MontarModificadores();
                                secoes.Add("E");

                                fimSecao.Add(secoes.Count);

                                //Limpo os dados
                                LOCCTR = 0;
                                reloc = 0;
                                nomeProg = instMatch.Groups["label"].Value;
                                ClearSYMTAB();
                                EXTDEF.Clear();
                                EXTREF.Clear();
                                break;

                            case "BASE":
                                BASE = true;
                                break;

                            case "NOBASE":
                                BASE = false;
                                break;

                        } //Fim do "switch(instMatch.Groups["dir"].Value)" 
                    } //Fim do "else" da pesquisa de instrução

                    //Pesquisa label
                    if (instMatch.Groups["label"].Value != "" && instMatch.Groups["dir"].Value != "EQU" && instMatch.Groups["dir"].Value != "CSECT")
                    {
                        try
                        {
                            SYMTAB.Add(instMatch.Groups["label"].Value, LOCCTR);
                        }
                        catch (Exception ex)
                        {
                            //Trata o erro no programa
                        }
                    } //Fim do if : pesquisa label
                } //Fim do "if (instMatch.Success)"
            } //Fim do "for (int i = 1; i < programa.Length; i++)"

            //Salvo os dados da seção atual
            MontarCabecalho(LOCCTR, reloc, nomeProg);
            MontarDefinicoesExt();
            MontarReferenciasExt();
            MontarInstrucoes();
            MontarModificadores();
            MontarFim(primeiraInst);
            fimSecao.Add(secoes.Count);

            //Salva em arquivo
            SalvarArquivoMontado();

            //Salva programa montado
            SalvarFonteMontado();
        }

        //Funções de montagem do texto do arquivo final em ASCII
        private void MontarCabecalho(int LOCCTR, int reloc, string nomeProg)
        {
            string esp = "      ";
            string zero = "000000";
            string LOCCTRHexa = String.Format("{0:X}", LOCCTR - reloc);
            string relocHexa = String.Format("{0:X}", reloc);

            string nomeConv = (nomeProg.Length < 6) ? (nomeProg + esp.Substring(0, 6 - nomeProg.Length)) : nomeProg.Substring(0, 6);
            string locctrConv = (LOCCTRHexa.Length < 6) ? (zero.Substring(0, 6 - LOCCTRHexa.Length) + LOCCTRHexa) : LOCCTRHexa;
            string relocConv = (relocHexa.Length < 6) ? (zero.Substring(0, 6 - relocHexa.Length) + relocHexa) : relocHexa;
            string strCabecalho = "H" + nomeConv + relocConv + locctrConv;
            secoes.Add(strCabecalho);
        }

        private void MontarDefinicoesExt()
        {
            if (EXTDEF.Count > 0)
            {
                string defTemp = "";

                foreach (var chave in EXTDEF.Keys)
                {
                    int pos = 0;
                    if (SYMTAB.TryGetValue(chave, out pos))
                    {
                        string esp = "      ";
                        string zero = "000000";
                        string posHexa = String.Format("{0:X}", pos);

                        string nomeConv = (chave.Length < 6) ? (chave + esp.Substring(0, 6 - chave.Length)) : chave.Substring(0, 6);
                        string posConv = (posHexa.Length < 6) ? (zero.Substring(0, 6 - posHexa.Length) + posHexa) : posHexa;
                        defTemp += nomeConv + posConv;
                    }
                    else
                    {
                        //ERRO no Programa
                    }
                }

                //Dividir as linhas se maiores que 73 caracteres
                do
                {
                    int qtdeCaracteres;

                    if (defTemp.Length > 72)
                        qtdeCaracteres = 72;
                    else
                        qtdeCaracteres = defTemp.Length;

                    string linha = "D" + defTemp.Substring(0, qtdeCaracteres);
                    defTemp = defTemp.Substring(qtdeCaracteres);
                    if (linha.Length > 1)
                        secoes.Add(linha);

                } while (defTemp.Length > 0);
            }
        }

        private void MontarReferenciasExt()
        {
            if (EXTREF.Count > 0)
            {
                string refTemp = "";


                foreach (var chave in EXTREF.Keys)
                {
                    string esp = "      ";

                    string nomeConv = (chave.Length < 6) ? (chave + esp.Substring(0, 6 - chave.Length)) : chave.Substring(0, 6);
                    //refTemp += nomeConv + ","; <<= retirada a "," do código fonte
                    refTemp += nomeConv;
                }

                //Retira a última vírgula da linha
                //refTemp = refTemp.Remove(refTemp.Length - 1);

                //Dividir as linhas se maiores que 73 caracteres
                do
                {
                    int qtdeCaracteres;

                    if (refTemp.Length > 72)
                        qtdeCaracteres = 72;
                    else
                        qtdeCaracteres = refTemp.Length;

                    string linha = "R" + refTemp.Substring(0, qtdeCaracteres);
                    refTemp = refTemp.Substring(qtdeCaracteres);
                    if (linha.Length > 1)
                        secoes.Add(linha);

                } while (refTemp.Length > 0);
            }
        }

        private void MontarInstrucoes()
        {
            string codFinal = "";
            string endRegistro = "";
            string zero = "000000"; //Conversão de valores para Hexa

            var LOCCTR_Local = instToProcess.Peek().LOCCTR;

            do
            {
                var linha = instToProcess.Dequeue();
                LinhaInstMont = linha.INST;
                LOCCTR = linha.LOCCTR;
                string codConvHexa = "";

                if (linha.isINST)
                {
                    string strINST = @"\s*(?<label>\S+\s+)?(?<tamES>\+)?(?<inst>" + Resource1.STR_INSTRUCOES + @")(\s+(?<param>(\S+\s*)*)?)?$";
                    Match matchInst = Regex.Match(linha.INST, strINST);
                    var param = matchInst.Groups["inst"].Value;
                    SetInstrucoes inst = setInstrucoes[matchInst.Groups["inst"].Value];

                    //Armazena a linha da instrução para acesso externo e chama função para montagem da instrução
                    int intCode = inst.Codigo;
                    var tempCode = String.Format("{0:X}", intCode);
                    var opCodeHexa = (tempCode.Length == 2)? (zero.Substring(0, 2 - tempCode.Length) + tempCode) : tempCode;

                    //Montagem da instrução
                    if (inst.Tamanho == 1)
                        codConvHexa = OPCodeINST1(opCodeHexa);
                    else if (inst.Tamanho == 2)
                        codConvHexa = OPCodeINST2(opCodeHexa);
                    else
                        codConvHexa = OPCodeINST3(intCode);

                    //Armazena o programa montado
                    {
                        string LOCCTRHexaM = String.Format("{0:X}", linha.LOCCTR);
                        var endRegistroM = (LOCCTRHexaM.Length < 4) ? (zero.Substring(0, 4 - LOCCTRHexaM.Length) + LOCCTRHexaM) : LOCCTRHexaM;
                        programaMontado[linha.nrLinha] = endRegistroM + "\t" + linha.INST + "\t\t\t" + codConvHexa;
                    }
                }
                else
                {
                    codConvHexa = linha.INST;
                }

                //Se estamos no começo da linha, armazenamos o endereço da primeira instrução
                if (endRegistro.Length == 0)
                {
                    string LOCCTRHexa = String.Format("{0:X}", linha.LOCCTR);
                    endRegistro = (LOCCTRHexa.Length < 6) ? (zero.Substring(0, 6 - LOCCTRHexa.Length) + LOCCTRHexa) : LOCCTRHexa;
                }

                //Se ainda cabe inserir mais cógido, insere, se não, armazena o código existente e depois insere
                /*
                 * Temos que testar também a continuidade do armazenamento do LOCCTR. Para armazenas as posições corretamente.
                 * Pois, se usamos uma instrução do tipo RESB 4096 o próximo valor a ser utilizado será  4K acima, então, precisamos criar
                 * outra linha em que a primeira variável obedeça a devida distância.
                 */
                if ((codFinal.Length + codConvHexa.Length) <= 60 && (linha.LOCCTR == LOCCTR_Local))
                {
                    codFinal += codConvHexa;
                }
                else
                {
                    //Pega o tamanho do registro a ser gravado
                    string tamRegHexa = String.Format("{0:X}", codFinal.Length / 2);
                    var tamRegistro = (tamRegHexa.Length < 2) ? (zero.Substring(0, 2 - tamRegHexa.Length) + tamRegHexa) : tamRegHexa;

                    //Insere os dados em um registro
                    secoes.Add("T" + endRegistro + tamRegistro + codFinal);

                    //Limpa os dados temporários
                    codFinal = "";
                    endRegistro = "";

                    //Agora insere os dados
                    string LOCCTRHexa2 = String.Format("{0:X}", linha.LOCCTR);
                    endRegistro = (LOCCTRHexa2.Length < 6) ? (zero.Substring(0, 6 - LOCCTRHexa2.Length) + LOCCTRHexa2) : LOCCTRHexa2;
                    codFinal += codConvHexa;

                    //Ajuste do LOCCTR_Local
                    LOCCTR_Local = linha.LOCCTR;
                }

                /*
                 * Incrementa o LOCCTR_Local com o tamanho da instrução armazenada
                 * Esse incremento é necessário para realizarmos corretamente o teste 
                 * e identificarmos saltos no locctr, logo, identificarmos declaração 
                 * de variáveis
                 */
                LOCCTR_Local += codConvHexa.Length / 2;

            } while (instToProcess.Count != 0);

            //Armazena algum código remanescente em codFinal
            if (codFinal.Length != 0)
            {
                //Pega o tamanho do registro a ser gravado
                string tamRegHexa = String.Format("{0:X}", codFinal.Length / 2);
                var tamRegistro = (tamRegHexa.Length < 2) ? (zero.Substring(0, 2 - tamRegHexa.Length) + tamRegHexa) : tamRegHexa;

                //Insere os dados em um registro
                secoes.Add("T" + endRegistro + tamRegistro + codFinal);
            }
        }

        private void MontarModificadores()
        {
            /*
             * A lista de modificadores é ordenada para que fique no mesmo
             * Padrão de ordem do livro. Se não for ordenada, os registros
             * de modificação de equações aparecerão no começo dos registros de modificação
             * e não no fim, como no livro.
             */
            modificadores.Sort();
            secoes.AddRange(modificadores);
            modificadores.Clear();
        }

        private void MontarFim(int primeiraInst)
        {
            string strFim;

            string zero = "000000";
            string posHexa = String.Format("{0:X}", primeiraInst);
            string posConv = (posHexa.Length < 6) ? (zero.Substring(0, 6 - posHexa.Length) + posHexa) : posHexa;
            strFim = "E" + posConv;

            if (fimSecao.Count > 0)
            {
                secoes.Add("E");
                secoes[fimSecao[0] - 1] = strFim;
            }
            else
                secoes.Add(strFim);
        }

        //Funções de montagem para cada tipo de instrução
        private string OPCodeINST1(string opcode)
        {
            int codInst = 0;
            string zero = "000000";

            if (SYMTAB.TryGetValue(LinhaInstMont.Trim(), out codInst))
            {
                string OPCodeHexa = String.Format("{0:X}", codInst);
                return ((OPCodeHexa.Length < 2) ? (zero.Substring(0, 2 - OPCodeHexa.Length) + OPCodeHexa) : OPCodeHexa);
            }
            else
                return "";
        }

        private string OPCodeINST2(string opcode)
        {
            int codInst;
            string OPMontada = opcode;

            string strINST = @"\s*(?<label>\S+)?\s+((?<inst>" + Resource1.STR_INSTRUCOES + @"))(\s+(?<param1>[^,\s]+)(,(?<param2>([^,\s]+|\d+)))?)?$";
            Match matchInst = Regex.Match(LinhaInstMont, strINST);

            var label = matchInst.Groups["label"].Value;
            var param1 = matchInst.Groups["param1"].Value;
            var param2 = matchInst.Groups["param2"].Value;

            if (opcode == "B0")    //Estamos lidando com a instrução SVC
            {
                string paramHexa = (param1.Length == 2) ? param1 : param1 + "0";
                OPMontada += paramHexa + ((sicCompativel) ? "00" : "");
            }
            else if (param2 == "")
            {
                SYMTAB.TryGetValue(param1, out codInst);

                string OPCodeHexa = String.Format("{0:X}", codInst);
                OPMontada += OPCodeHexa + "0";
            }
            else
            {
                int reg1, reg2;

                SYMTAB.TryGetValue(matchInst.Groups["param1"].Value, out reg1);

                if (Regex.IsMatch(param2, @"\d+"))
                    Int32.TryParse(param2, out reg2);
                else
                    SYMTAB.TryGetValue(param2, out reg2);

                string reg1Hexa = String.Format("{0:X}", reg1);
                string reg2Hexa = String.Format("{0:X}", reg2);

                OPMontada += reg1Hexa + reg2Hexa;
            }

            return OPMontada;
        }

        private string OPCodeINST3(int codInst)
        {
            string OPMontada = "";
            int tamInstLocal = 3;
            string strINST = @"\s*((?<label>\S+)?\s+(?<tamES>\+)?)?((?<inst>" + Resource1.STR_INSTRUCOES + @"))(\s+(?<param>(\S+\s*[^\r\n\t])*)?)?$";
            Match matchInst = Regex.Match(LinhaInstMont, strINST);

            var label = matchInst.Groups["label"].Value;
            var tamES = matchInst.Groups["tamES"].Value;
            var inst = matchInst.Groups["inst"].Value;
            var param = matchInst.Groups["param"].Value;

            string zero = "000000";

            if (tamES == "+")
                tamInstLocal++;

            if (param.StartsWith("#"))    //endereçamento imediato
            {
                //variável qualquer para receber o valor da SYMTAB...
                int descartavel;


                //N = 0, I = 1
                codInst += 1;

                //XBPE = 0000
                codInst = codInst << 4;

                //Se tamanho = 4: E = 1
                if (tamInstLocal == 4)
                    codInst++;
                else if (SYMTAB.TryGetValue(param.Substring(1, param.Length - 1), out descartavel))
                {
                    //Se estamos tratando com variáveis, então:
                    //Verifica se é relativo à BASE ou PC
                    if (BASE)
                        codInst += 4;
                    else
                        codInst += 2;
                }

                string OPCodeHexa = String.Format("{0:X}", codInst);
                OPMontada += (OPCodeHexa.Length < 3) ? (zero.Substring(0, 3 - OPCodeHexa.Length) + OPCodeHexa) : OPCodeHexa.Substring(0,3);
                codInst = 0;

                if (!SYMTAB.TryGetValue(param.Substring(1, param.Length - 1), out codInst))
                    codInst = Int32.Parse(param.Substring(1)/*, System.Globalization.NumberStyles.AllowHexSpecifier*/);
                else
                    codInst -= LOCCTR;

                string OPEndHexa = String.Format("{0:X}", codInst);

                if (tamInstLocal == 3)
                    OPMontada += (OPEndHexa.Length < 3) ? (zero.Substring(0, 3 - OPEndHexa.Length) + OPEndHexa) : OPEndHexa.Substring(OPEndHexa.Length - 3, 3);
                else
                    OPMontada += (OPEndHexa.Length < 5) ? (zero.Substring(0, 5 - OPEndHexa.Length) + OPEndHexa) : OPEndHexa.Substring(OPEndHexa.Length - 5, 5);
            }
            else if (param.StartsWith("="))
            {
                //Match matchLit = Regex.Match(param, @"=(X|)");
            }
            else if (param.StartsWith("@"))   //endereçamento indireto
            {
                //Adiciona N=1 e I=0
                codInst += 2;

                //XBPE 0000
                codInst = codInst << 4;

                //Se tamanho = 4: E = 1
                if (tamInstLocal == 4)
                    codInst++;

                //Verifica se é relativo à BASE ou PC
                else if (BASE)
                    codInst += 4;
                else
                    codInst += 2;

                //Armazena os 3 primeiros caracteres em hexa
                string OPCodeHexa = String.Format("{0:X}", codInst);
                OPMontada += (OPCodeHexa.Length < 3) ? (zero.Substring(0, 3 - OPCodeHexa.Length) + OPCodeHexa) : OPCodeHexa.Substring(0, 3);
                codInst = 0;


                //Verifica o endereço de memória
                int posMemProg;
                string var = param.Substring(1); //remove o @

                if (EXTREF.ContainsKey(var))    //Se referência externa -> end. = 0
                    codInst = 0;
                else if (tamInstLocal == 4 && SYMTAB.TryGetValue(var, out posMemProg)) //Tamanho extendido pega o end. diretamente
                {
                    codInst = posMemProg;
                }
                else if (SYMTAB.TryGetValue(var, out posMemProg))  //Se é uma posição de memória
                {
                    if (BASE)
                    {
                        codInst = posMemProg /*- sic.SYMTAB["B"]*/;
                    }
                    else
                    {
                        codInst = posMemProg - LOCCTR - tamInstLocal;
                    }
                }
                else
                {
                    //Erro no programa
                }

                //Armazena o valor de codInt
                string OPEndHexa = String.Format("{0:X}", codInst);

                if (tamInstLocal == 3)
                    OPMontada += (OPEndHexa.Length < 3) ? (zero.Substring(0, 3 - OPEndHexa.Length) + OPEndHexa) : OPEndHexa.Substring(OPEndHexa.Length - 3, 3);
                else
                    OPMontada += (OPEndHexa.Length < 5) ? (zero.Substring(0, 5 - OPEndHexa.Length) + OPEndHexa) : OPEndHexa.Substring(OPEndHexa.Length - 5, 5);
            }
            else    //endereço de memória
            {
                bool indexado = false;
                bool peloPC = false;
                bool extendido = false;

                
                if (!sicCompativel)
                {
                    codInst += 3;   //Adiciona N=1 e I=1

                    //XBPE
                    codInst = codInst << 4;

                    //Verifica se é indexado
                    if (param.EndsWith(",X"))
                    {
                        indexado = true;
                        codInst += 8;
                    }

                    //Se tamanho = 4: E = 1
                    if (tamInstLocal == 4)
                    {
                        extendido = true;
                        codInst++;
                    }

                    //Verifica se é relativo à BASE ou PC
                    /*
                     * Se for uma instrução padrão SIC que esteja sendo montada em um objeto padrão SIC/XE
                     * então o teste deve ser feito para correta inserção dos flags P e B
                     */                    
                    else if (inst != "RSUB")
                    {
                        if (BASE)
                        {
                            peloPC = false;
                            codInst += 4;
                        }
                        else
                        {
                            peloPC = true;
                            codInst += 2;
                        }
                    }

                    //Armazena os 3 primeiros caracteres em hexa
                    string OPCodeHexa = String.Format("{0:X}", codInst);
                    OPMontada += (OPCodeHexa.Length < 3) ? (zero.Substring(0, 3 - OPCodeHexa.Length) + OPCodeHexa) : OPCodeHexa.Substring(0, 3);
                    codInst = 0;
                    //Verifica o endereço de memória
                    int posMemProg;
                    string endMem = param;
                    endMem = (indexado) ? endMem.Remove(endMem.Length - 2) : endMem;    //Remove o ",X" do final

                    //Monta o endereço da memória e registros de modificação
                    if (EXTREF.ContainsKey(endMem))
                    {
                        //Registro de modificação
                        var locctrHexa = String.Format("{0:X}", LOCCTR + 1);
                        locctrHexa = (locctrHexa.Length < 6) ? (zero.Substring(0, 6 - locctrHexa.Length) + locctrHexa) : locctrHexa.Substring(0, 6);
                        string qtdeMeioBit = (extendido) ? "05" : "03";

                        string SPC = "      ";
                        string endMemSPC = (endMem.Length == 6) ? endMem : endMem + SPC.Substring(0, 6 - endMem.Length);
                        modificadores.Add("M" + locctrHexa + qtdeMeioBit + "+" + endMemSPC);
                    }
                    else if (SYMTAB.TryGetValue(endMem, out posMemProg))
                    {
                        if (peloPC)
                            codInst = posMemProg - LOCCTR - tamInstLocal;

                        else
                            codInst = posMemProg /*- sic.SYB["B"]*/;

                        //Deve ser na parte do SIC
                        //Registro de modificação
                        //if (sicCompativel)
                        //{
                        //    var locctrHexa = String.Format("{0:X}", LOCCTR + 1);
                        //    locctrHexa = (locctrHexa.Length < 6) ? (zero.Substring(0, 6 - locctrHexa.Length) + locctrHexa) : locctrHexa.Substring(0, 6);
                        //    string qtdeMeioBit = (extendido) ? "05" : "03";
                        //    modificadores.Add("M" + locctrHexa + qtdeMeioBit);
                        //}
                    }


                    //Armazena o valor de codInt
                    string OPEndHexa = String.Format("{0:X}", codInst);

                    if (!extendido)
                        OPMontada += (OPEndHexa.Length <= 3) ? (zero.Substring(0, 3 - OPEndHexa.Length) + OPEndHexa) : OPEndHexa.Substring(OPEndHexa.Length - 3, 3);
                    else
                        OPMontada += (OPEndHexa.Length <= 5) ? (zero.Substring(0, 5 - OPEndHexa.Length) + OPEndHexa) : OPEndHexa.Substring(OPEndHexa.Length - 5, 5);
                }
                else //Instrução formato 3 - padrão SIC
                {
                    //Armazena os 2 primeiros caracteres em hexa
                    string OPCodeHexa = String.Format("{0:X}", codInst);
                    OPMontada += (OPCodeHexa.Length < 2) ? (zero.Substring(0, 2 - OPCodeHexa.Length) + OPCodeHexa) : OPCodeHexa.Substring(0, 2);
                    codInst = 0;

                    //Verifica se é indexado
                    if (param.EndsWith(",X"))
                        indexado = true;

                    string endMem = param;
                    endMem = (indexado) ? endMem.Remove(endMem.Length - 2) : endMem;    //Remove o ",X" do final

                    SYMTAB.TryGetValue(endMem, out codInst);

                    codInst += (indexado) ? 0x8000 : 0;


                    string OPEndHexa = String.Format("{0:X}", codInst);
                    OPMontada += (OPEndHexa.Length <= 4) ? (zero.Substring(0, 4 - OPEndHexa.Length) + OPEndHexa) : OPEndHexa.Substring(OPEndHexa.Length - 4, 4);

                    //Registro de modificação
                    if (reloc == 0)
                    {
                        var locctrHexa = String.Format("{0:X}", LOCCTR + 1);
                        locctrHexa = (locctrHexa.Length < 6) ? (zero.Substring(0, 6 - locctrHexa.Length) + locctrHexa) : locctrHexa.Substring(0, 6);
                        string qtdeMeioBit = (extendido) ? "05" : "03";
                        modificadores.Add("M" + locctrHexa + qtdeMeioBit);
                    }
                }
            }

            return OPMontada;
        }

        //Funções Utilitárias
        private void SalvarArquivoMontado()
        {
            BinaryWriter arquivo;
            int indiceSecoes = 0;

            for (int i = 0; i < fimSecao.Count; i++)
            {
                arquivo = new BinaryWriter(File.OpenWrite(dirPath + secoes[indiceSecoes].Substring(1, 6).Trim() + ".sic"));

                while (indiceSecoes < fimSecao[i])
                {
                    string linha = secoes[indiceSecoes];
                    char inic = linha[0];

                    switch (inic)
                    {
                        case 'H':
                            //Cabeçalho = 1B
                            arquivo.Write((byte)linha[0]);

                            //Nome do programa = 6B
                            for (int n = 1; n <= 6; n++)
                                arquivo.Write((byte)linha[n]);

                            //Endereço inicial = 3B
                            for (int e = 7; e < 12; e += 2)
                                arquivo.Write(Byte.Parse(linha.Substring(e,2), NumberStyles.AllowHexSpecifier));

                            //Tamanho do Programa = 3B
                            for (int t = 13; t < 18; t += 2)
                                arquivo.Write(Byte.Parse(linha.Substring(t, 2), NumberStyles.AllowHexSpecifier));
                            break;

                        case 'D':
                            //Cabeçalho = 1B
                            arquivo.Write((byte)linha[0]);

                            //Nome da variável e posição
                            int posD = 1;

                            do
                            {
                                //Escreve o nome da variável
                                for (int n = 0; n < 6; n++)
                                    arquivo.Write((byte)linha[n + posD]);

                                //Escreve o endereço da variável
                                for (int e = 0; e < 6; e += 2)
                                    arquivo.Write(Byte.Parse(linha.Substring(e + 6 + posD, 2), NumberStyles.AllowHexSpecifier));

                                posD += 12; 

                            } while (posD < linha.Length);

                            break;

                        case 'R':
                            //Cabeçalho = 1B
                            arquivo.Write((byte)linha[0]);

                            //Nome da variável e posição
                            int posR = 1;

                            do
                            {
                                //Escreve o nome da variável
                                for (int n = 0; n < 6; n++)
                                    arquivo.Write((byte)linha[n + posR]);

                                //+7 = 6 caracteres do nome + ","
                                //posR += 7;

                                //retirada a "," do código objeto
                                posR += 6;

                            } while (posR < linha.Length);

                            break;

                        case 'T':
                            //Cabeçalho = 1B
                            arquivo.Write((byte)linha[0]);

                            for (int b = 1; b < linha.Length; b += 2)
                                arquivo.Write(Byte.Parse(linha.Substring(b, 2), NumberStyles.AllowHexSpecifier));

                            break;

                        case 'M':
                            //Cabeçalho = 1B
                            arquivo.Write((byte)linha[0]);

                            //Escreve o endereço da variável
                            for (int e = 1; e < 6; e += 2)
                                arquivo.Write(Byte.Parse(linha.Substring(e, 2), NumberStyles.AllowHexSpecifier));

                            //Escreve a quantidade de meio bytes
                            arquivo.Write(Byte.Parse(linha.Substring(7, 2), NumberStyles.AllowHexSpecifier));

                            if (linha.Length > 9)
                            {
                                //Flag de modificação
                                arquivo.Write((byte)linha[9]);

                                //Escreve o nome da variável
                                for (int n = 10; n < linha.Length; n++)
                                    arquivo.Write((byte)linha[n]);
                            }
                            break;

                        case 'E':
                            //Cabeçalho = 1B
                            arquivo.Write((byte)linha[0]);

                            if (linha.Length >= 7)
                                for (int e = 1; e < 7; e += 2)
                                    arquivo.Write(Byte.Parse(linha.Substring(e, 2), NumberStyles.AllowHexSpecifier));

                            break;

                        default:
                            break;
                    }

                    //Acrescentado o fim de linha para facilitar o algoritmo de carga do LinkLoader
                    arquivo.Write((byte)10);

                    indiceSecoes++;
                }

                arquivo.Close();
            }

            SalvarArquivoMontadoASCII();
            MessageBox.Show("Arquivo(s) Montado(s)", "Salvar Arquivo Objeto", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SalvarArquivoMontadoASCII()
        {
            TextWriter arquivo;
            int indiceSecoes = 0;

            for (int i = 0; i < fimSecao.Count; i++)
            {
                arquivo = new StreamWriter(dirPath + secoes[indiceSecoes].Substring(1, 6).Trim() + ".asic");

                while (indiceSecoes < fimSecao[i])
                {
                    arquivo.WriteLine(secoes[indiceSecoes]);
                    indiceSecoes++;
                }

                arquivo.Close();
            }
        }

        private void SalvarFonteMontado()
        {
            TextWriter arquivo;
            MessageBoxResult result = MessageBoxResult.No;
            int indiceSecoes = 0;


            result = MessageBox.Show("Você gostaria de salvar o programa montado?",
                                        "Salvar Arquivo Fonte",
                                        MessageBoxButton.YesNo,
                                        MessageBoxImage.Question,
                                        MessageBoxResult.Yes);

            if (result == MessageBoxResult.Yes)
            {
                for (int i = 0; i < fimSecao.Count; i++)
                {
                    arquivo = new StreamWriter(dirPath + secoes[indiceSecoes].Substring(1, 6).Trim() + " Montado" + ".asm");

                    foreach (var linha in programaMontado.ToArray())
                    {
                        arquivo.WriteLine(linha);
                    }

                    arquivo.Close();
                }
            }

            MessageBox.Show("Arquivo(s) Salvo(s)", "Salvar Arquivo Objeto", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearSYMTAB()
        {
            SYMTAB.Clear();
            SYMTAB.Add("A", 0);
            SYMTAB.Add("X", 1);
            SYMTAB.Add("L", 2);
            SYMTAB.Add("B", 3);
            SYMTAB.Add("S", 4);
            SYMTAB.Add("T", 5);
            SYMTAB.Add("F", 6);
            SYMTAB.Add("PC", 8);
            SYMTAB.Add("SW", 9);
        }

        private void LoadInstrucoes()
        {
            setInstrucoes = new Dictionary<string, SetInstrucoes>();

            setInstrucoes.Add("AADF", new AADF());
            setInstrucoes.Add("ADD", new ADD());
            setInstrucoes.Add("ADDR", new ADDR());
            setInstrucoes.Add("AND", new AND());
            setInstrucoes.Add("CLEAR", new CLEAR());
            setInstrucoes.Add("COMP", new COMP());
            setInstrucoes.Add("COMPR", new COMPR());
            setInstrucoes.Add("COMPF", new COMPF());
            setInstrucoes.Add("DIV", new DIV());
            setInstrucoes.Add("DIVF", new DIVF());
            setInstrucoes.Add("DIVR", new DIVR());
            setInstrucoes.Add("FIX", new FIX());
            setInstrucoes.Add("FLOAT", new FLOAT());
            setInstrucoes.Add("HIO", new HIO());
            setInstrucoes.Add("J", new J());
            setInstrucoes.Add("JEQ", new JEQ());
            setInstrucoes.Add("JGT", new JGT());
            setInstrucoes.Add("JLT", new JLT());
            setInstrucoes.Add("JSUB", new JSUB());
            setInstrucoes.Add("LDA", new LDA());
            setInstrucoes.Add("LDB", new LDB());
            setInstrucoes.Add("LDCH", new LDCH());
            setInstrucoes.Add("LDL", new LDL());
            setInstrucoes.Add("LDF", new LDF());
            setInstrucoes.Add("LDS", new LDS());
            setInstrucoes.Add("LDT", new LDT());
            setInstrucoes.Add("LDX", new LDX());
            setInstrucoes.Add("LPS", new LPS());
            setInstrucoes.Add("MUL", new MUL());
            setInstrucoes.Add("MULR", new MULR());
            setInstrucoes.Add("MULF", new MULF());
            setInstrucoes.Add("OR", new OR());
            setInstrucoes.Add("RD", new RD());
            setInstrucoes.Add("RMO", new RMO());
            setInstrucoes.Add("RSUB", new RSUB());
            setInstrucoes.Add("SHIFTL", new SHIFTL());
            setInstrucoes.Add("SHIFTR", new SHIFTR());
            setInstrucoes.Add("SIO", new SIO());
            setInstrucoes.Add("SSK", new SSK());
            setInstrucoes.Add("STA", new STA());
            setInstrucoes.Add("STB", new STB());
            setInstrucoes.Add("STCH", new STCH());
            setInstrucoes.Add("STI", new STI());
            setInstrucoes.Add("STL", new STL());
            setInstrucoes.Add("STS", new STS());
            setInstrucoes.Add("STSW", new STSW());
            setInstrucoes.Add("STT", new STT());
            setInstrucoes.Add("STX", new STX());
            setInstrucoes.Add("SUB", new SUB());
            setInstrucoes.Add("SUBR", new SUBR());
            setInstrucoes.Add("SVC", new SVC());
            setInstrucoes.Add("TD", new TD());
            setInstrucoes.Add("TIO", new TIO());
            setInstrucoes.Add("TIX", new TIX());
            setInstrucoes.Add("TIXR", new TIXR());
            setInstrucoes.Add("WD", new WD());
        }
    }
}

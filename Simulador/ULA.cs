using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Simulador
{
    class ULA
    {
        public struct Flags
        {
            public bool N;
            public bool I;
            public bool X;
            public bool B;
            public bool P;
            public bool E;
        }

        public Memoria memoria;
        public ControladorES dispES;
        Dictionary<int, SetInstrucoes> setInstrucoes;
        public Dictionary<int, long> registradores;
        public Flags flags;
        public long parametroCarregado;
        public int enderecoCarregado;
        public int timer;
        public SetInstrucoes instrucao;
        public string enderecamento;
        public int Tamanho;

        //Registradores
        //usado só para facilitar o acesso ao dicionário
        //acessando o registrador pelo nome ao invés do endereço
        public readonly int A = 0;
        public readonly int X = 1;
        public readonly int L = 2;
        public readonly int B = 3;
        public readonly int S = 4;
        public readonly int T = 5;
        public readonly int F = 6;
        public readonly int PC = 8;
        public readonly int SW = 9;        

        /// <summary>
        /// Cria uma ULA.
        /// </summary>
        /// <param name="memoria">referência para o controlador de memória</param>
        public ULA(ref Memoria memoria, ref ControladorES dispES)
        {
            this.memoria = memoria;
            this.dispES = dispES;

            LoadInstrucoes();
            LoadRegistradores();
        }

        /// <summary>
        /// Executa a instrução apontada por PC e incrementa o mesmo
        /// </summary>
        public bool Execute()
        {
            //Endereço da próxima instrução
            int endProx = (int)registradores[PC];

            //Verifica qual é a instrução
            byte instrucaoCod = memoria[endProx];
            instrucaoCod = (byte)(instrucaoCod & 0xFC);            

            if (!setInstrucoes.TryGetValue(instrucaoCod, out instrucao))
            {
                //TODO: tratar fim do código executável ou erro
                System.Windows.MessageBox.Show("Instrução inválida!", "Simulador SIC/XE");
                return false;
            }
            else if (instrucao.Codigo == 0xB0)
            {
                //Estamos utilizando a interrupção SVC para indicar o encerramento do programa
                System.Windows.MessageBox.Show("Fim do programa encontrado!", "Simulador SIC/XE");
                return false;
            }

            //Faz o tratamento dos flags e o incremento adequado para PC em cada tamanho de instrução
            //Utilizamos o "long" parametroCarregado para não precisar fazer mais teste e mais buscas na memória para o parâmetro
            //extra em cada execução. A busca só é feita se for realmente necessário
            switch (instrucao.Tamanho)
            {
                case 1:
                    //Não precisa fazer tratamento nenhum antes de executar
                    //Instruções tam = 1 não utilizam parâmetros
                    registradores[PC]++;
                    instrucao.Execute(this);
                    enderecamento = "não há endereçamento.";
                    Tamanho = 1;
                    break;

                case 2:
                    //Não precisa fazer tratamento nenhum antes de executar
                    //Enviamos só o parâmetro da instrução pela "instrucaoCarregada"
                    parametroCarregado = memoria.Read(endProx + 1, 1);
                    registradores[PC] += 2;
                    instrucao.Execute(this);
                    enderecamento = "endereçamento de registradores.";
                    Tamanho = 2;
                    break;

                case 3:
                    //Faz o tratamento dos flags
                    flags.N = ((memoria[endProx] & 2) == 2) ? true : false;
                    flags.I = ((memoria[endProx] & 1) == 1) ? true : false;
                    flags.X = ((memoria[endProx + 1] & 0x80) == 0x80) ? true : false;
                    flags.B = ((memoria[endProx + 1] & 0x40) == 0x40) ? true : false;
                    flags.P = ((memoria[endProx + 1] & 0x20) == 0x20) ? true : false;
                    flags.E = ((memoria[endProx + 1] & 0x10) == 0x10) ? true : false;

                    //Variavel auxiliar para armazenar o parâmetro

                    if (flags.E)
                    {
                        //Enviamos só o parâmetro da instrução pela "instrucaoCarregada"
                        parametroCarregado = memoria.Read(endProx + 1, 3) & 0x0FFFFF;
                        registradores[PC] += 4;
                        Tamanho = 4;
                    }
                    else
                    {
                        //Se é tamanho 3 e N = 0 e I = 0, então é formato SIC Standard
                        //Então os 3 bits finais passam a ser endereços
                        //Caso contrário, retiramos os bits dos flags
                        if (!(flags.N && flags.I))
                            parametroCarregado = memoria.Read(endProx + 1, 2) & 0x7FFF;
                        else
                            parametroCarregado = memoria.Read(endProx + 1, 2) & 0x0FFF;
                        registradores[PC] += 3;

                        Tamanho = 3;
                    }

                    int tam;

                    //verifica se é uma instrução de inteiro ou ponto flutuante
                    //para definir qual a quantidade de bytes na memória a serem lidos/escritos
                    if (instrucao.Codigo == 0x58 || //AADF
                        instrucao.Codigo == 0x88 || //COMPF
                        instrucao.Codigo == 0x64 || //DIVF
                        instrucao.Codigo == 0xC4 || //FIX
                        instrucao.Codigo == 0x70 || //LDF
                        instrucao.Codigo == 0x60 || //MULF
                        instrucao.Codigo == 0xC8 || //NORM
                        instrucao.Codigo == 0x80 || //STF
                        instrucao.Codigo == 0x5C)   //SUBF
                        
                        tam = 6;

                    else if (instrucao.Codigo == 0x50 || //LDCH
                             instrucao.Codigo == 0xD8 || //RD
                             instrucao.Codigo == 0x54 || //STCH
                             instrucao.Codigo == 0xE0 ||//TD
                             instrucao.Codigo == 0xDC)
                        tam = 1;

                    else
                        tam = 3;

                    //Faz a busca e possível conversão do parâmetro de acordo com os flags da instrução
                    long valorOperando = parametroCarregado;

                    //N = 0 e I = 0 : Formato SIC standard
                    if (!flags.N && !flags.I)
                    {
                        //oper. m,x
                        if (flags.X)
                        {
                            enderecoCarregado = (int)(parametroCarregado + registradores[X]);
                            valorOperando = memoria.Read(enderecoCarregado, tam);
                        }
                        //oper. m
                        else
                        {
                            enderecoCarregado = (int)parametroCarregado;
                            valorOperando = memoria.Read(enderecoCarregado, tam);
                        }

                        enderecamento = "endereçamento SIC Standard.";
                    }
                    //N = 0 e I = 1 : (#) Endereçamento Imediato
                    else if (!flags.N && flags.I)
                    {
                        //oper.#c
                        if (!flags.X && !flags.B && !flags.P && !flags.E)
                        {
                            enderecoCarregado = (int)parametroCarregado;
                            valorOperando = parametroCarregado;
                            enderecamento = "end. imediato: oper. #c";
                        }
                        //+oper.#m
                        else if (!flags.X && !flags.B && !flags.P && flags.E)
                        {
                            enderecoCarregado = (int)parametroCarregado;
                            valorOperando = parametroCarregado;
                            enderecamento = "end. imediato: +oper. #m";
                        }
                        //oper.#m (PC)
                        else if (!flags.X && !flags.B && flags.P && !flags.E)
                        {
                            enderecoCarregado = (int)(Word.Convert12BitsSigned(parametroCarregado) + registradores[PC]);
                            valorOperando = Word.Convert12BitsSigned(parametroCarregado) + registradores[PC];
                            enderecamento = "end. imediato: oper. #m (PC)";
                        }
                        //oper.#m (B)
                        else if (!flags.X && !flags.B && flags.P && !flags.E)
                        {
                            enderecoCarregado = (int)(parametroCarregado + registradores[B]);
                            valorOperando = parametroCarregado + registradores[B];
                            enderecamento = "end. imediato: oper. #m (B)";
                        }
                        else
                        {
                            //TODO: tratar erro nos flags
                        }
                    }
                    //N = 1 e I = 0 : (@) Endereçamento Indireto
                    else if (flags.N && !flags.I)
                    {
                        //oper.@c
                        if (!flags.X && !flags.B && !flags.P && !flags.E)
                        {
                            enderecoCarregado = (int)memoria[(int)parametroCarregado];
                            valorOperando = memoria.Read(enderecoCarregado, tam);
                            enderecamento = "end. indireto: oper. @c";
                        }
                        //+oper.@m
                        else if (!flags.X && !flags.B && !flags.P && flags.E)
                        {
                            enderecoCarregado = (int)memoria[(int)parametroCarregado];
                            valorOperando = memoria.Read(enderecoCarregado, tam);
                            enderecamento = "end. indireto: +oper. @m";
                        }
                        //oper.@m (PC)
                        else if (!flags.X && !flags.B && flags.P && !flags.E)
                        {
                            enderecoCarregado = (int)memoria[(int)(Word.Convert12BitsSigned(parametroCarregado) + registradores[PC])];
                            valorOperando = memoria.Read(enderecoCarregado, tam);
                            enderecamento = "end. indireto: oper. @m (PC)";
                        }
                        //oper.@m (B)
                        else if (!flags.X && !flags.B && flags.P && !flags.E)
                        {
                            enderecoCarregado = (int)memoria[(int)(parametroCarregado + registradores[B])];
                            valorOperando = memoria.Read(enderecoCarregado, tam);
                            enderecamento = "end. indireto: oper. @m (B)";
                        }
                        else
                        {
                            //TODO: tratar erro nos flags
                        }
                    }
                    //N = 1 e I = 1 : [] Endereçamento de memória
                    else if (flags.N && flags.I)
                    {
                        //oper.c
                        if (!flags.X && !flags.B && !flags.P && !flags.E)
                        {
                            enderecoCarregado = (int)parametroCarregado;
                            valorOperando = memoria.Read(enderecoCarregado, tam);
                            enderecamento = "end. de memória: oper. c";
                        }
                        //+oper.m
                        else if (!flags.X && !flags.B && !flags.P && flags.E)
                        {
                            enderecoCarregado = (int)parametroCarregado;
                            valorOperando = memoria.Read(enderecoCarregado, tam);
                            enderecamento = "end. de memória: +oper. m";
                        }
                        //oper.m (PC)
                        else if (!flags.X && !flags.B && flags.P && !flags.E)
                        {
                            enderecoCarregado = (int)(Word.Convert12BitsSigned(parametroCarregado) + registradores[PC]);
                            valorOperando = memoria.Read(enderecoCarregado, tam);
                            enderecamento = "end. de memória: oper. m (PC)";
                        }
                        //oper. c. (B)
                        else if (!flags.X && flags.B && !flags.P && !flags.E)
                        {
                            enderecoCarregado = (int)(parametroCarregado + registradores[B]);
                            valorOperando = memoria.Read(enderecoCarregado, tam);
                            enderecamento = "end. de memória: oper. c (B)";
                        }
                        //+oper.m, x
                        else if (flags.X && !flags.B && !flags.P && !flags.E)
                        {
                            enderecoCarregado = (int)(parametroCarregado + registradores[X]);
                            valorOperando = memoria.Read(enderecoCarregado, tam);
                            enderecamento = "end. de memória: oper. m, x";
                        }
                        //oper. m,x
                        else if (flags.X && !flags.B && !flags.P && flags.E)
                        {
                            enderecoCarregado = (int)(parametroCarregado + registradores[X]);
                            valorOperando = memoria.Read(enderecoCarregado, tam);
                            enderecamento = "end. de memória: oper. m, x";
                        }
                        //oper. m, x (PC)
                        else if (flags.X && !flags.B && flags.P && !flags.E)
                        {
                            enderecoCarregado = (int)(Word.Convert12BitsSigned(parametroCarregado) + registradores[PC] + registradores[X]);
                            valorOperando = memoria.Read(enderecoCarregado, tam);
                            enderecamento = "end. de memória: oper. m, x (PC)";
                        }
                        //oper. m, x (B)
                        else if (flags.X && flags.B && !flags.P && !flags.E)
                        {
                            enderecoCarregado = (int)(parametroCarregado + registradores[B] + registradores[X]);
                            valorOperando = memoria.Read(enderecoCarregado, tam);
                            enderecamento = "end. de memória: oper. m, x (B)";
                        }
                        else
                        {
                            //TODO: tratar erro nos flags
                        }
                    }
                    else
                    {
                        //TODO: tratar erro na instrução
                    }

                    //recarrega o parametroCarregado com o valor correto do operando.
                    parametroCarregado = valorOperando;

                    instrucao.Execute(this);
                    break;

                default:
                    //TODO: tratar erro
                    break;
            }

            return true;
        }

        private void LoadRegistradores()
        {
            registradores = new Dictionary<int, long>();
            registradores.Add(0, 0);    //A
            registradores.Add(1, 0);    //X
            registradores.Add(2, 0);    //L
            registradores.Add(3, 0);    //B
            registradores.Add(4, 0);    //S
            registradores.Add(5, 0);    //T
            registradores.Add(6, 0);    //F
            registradores.Add(8, 0);    //PC
            registradores.Add(9, 0);    //SW
        }

        private void LoadInstrucoes()
        {
            setInstrucoes = new Dictionary<int, SetInstrucoes>();

            setInstrucoes.Add(0x18, new ADD());
            setInstrucoes.Add(0x90, new ADDR());
            setInstrucoes.Add(0x40, new AND());
            setInstrucoes.Add(0xB4, new CLEAR());
            setInstrucoes.Add(0x28, new COMP());
            setInstrucoes.Add(0xA0, new COMPR());
            setInstrucoes.Add(0x24, new DIV());
            setInstrucoes.Add(0x9C, new DIVR());
            setInstrucoes.Add(0xF4, new HIO());
            setInstrucoes.Add(0x3C, new J());
            setInstrucoes.Add(0x30, new JEQ());
            setInstrucoes.Add(0x34, new JGT());
            setInstrucoes.Add(0x38, new JLT());
            setInstrucoes.Add(0x48, new JSUB());
            setInstrucoes.Add(0x00, new LDA());
            setInstrucoes.Add(0x68, new LDB());
            setInstrucoes.Add(0x50, new LDCH());
            setInstrucoes.Add(0x08, new LDL());
            setInstrucoes.Add(0x70, new LDF());
            setInstrucoes.Add(0x6C, new LDS());
            setInstrucoes.Add(0x74, new LDT());
            setInstrucoes.Add(0x04, new LDX());
            setInstrucoes.Add(0xD0, new LPS());
            setInstrucoes.Add(0x20, new MUL());
            setInstrucoes.Add(0x98, new MULR());
            setInstrucoes.Add(0x44, new OR());
            setInstrucoes.Add(0xD8, new RD());
            setInstrucoes.Add(0xAC, new RMO());
            setInstrucoes.Add(0x4C, new RSUB());
            setInstrucoes.Add(0xA4, new SHIFTL());
            setInstrucoes.Add(0xA8, new SHIFTR());
            setInstrucoes.Add(0xF0, new SIO());
            setInstrucoes.Add(0xEC, new SSK());
            setInstrucoes.Add(0x0C, new STA());
            setInstrucoes.Add(0x78, new STB());
            setInstrucoes.Add(0x54, new STCH());
            setInstrucoes.Add(0xD4, new STI());
            setInstrucoes.Add(0x14, new STL());
            setInstrucoes.Add(0x7C, new STS());
            setInstrucoes.Add(0xE8, new STSW());
            setInstrucoes.Add(0x84, new STT());
            setInstrucoes.Add(0x10, new STX());
            setInstrucoes.Add(0x1C, new SUB());
            setInstrucoes.Add(0x94, new SUBR());
            setInstrucoes.Add(0xB0, new SVC());
            setInstrucoes.Add(0xE0, new TD());
            setInstrucoes.Add(0xF8, new TIO());
            setInstrucoes.Add(0x2C, new TIX());
            setInstrucoes.Add(0xB8, new TIXR());
            setInstrucoes.Add(0xDC, new WD());
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
 
namespace Simulador
{
    public delegate void MemModifDelegate(int pos, int qtde);

    class Memoria
    {
        public const int TAM_MEM = 1048576;
        public event MemModifDelegate PosModif;

        private byte[] memoria;
        private byte[] chave;
        private byte[] mapaBits;

        public Memoria()
        {
            memoria = new byte[TAM_MEM];

            //Chaves de acesso à memória. Para instrução SSK
            chave = new byte[TAM_MEM];

            //Mapa de bits para verificar os espaços livres na memória e lugares para alocação
            //e carga de programas. Implementado em um vetor
            mapaBits = new byte[TAM_MEM];
        }

        /// <summary>
        /// Lê na memória a partir de uma posição e retorna o valor lido. A leitura é realizada a partir
        /// do MSB, ou seja, a posição especificada terá o MSB
        /// </summary>
        /// <param name="posD">Posição inicial de leitura da memória</param>
        /// <param name="tam">Quantidade de bytes que deverão ser lidos</param>
        /// <returns>Valor concatenado lido da memória</returns>
        public long Read(int pos, int tam)
        {
            try
            {
                long valor = long.MaxValue;

                //Se a posição a ser lida está dentro dos limites da memória
                if (pos >= 0 && pos <= TAM_MEM - tam)
                {
                    valor = memoria[pos];

                    for (int i = 1; i < tam; i++)
                    {
                        valor = valor << 8;
                        valor += memoria[pos + i];
                    }
                }

                return valor;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Erro no endereçamento de memória. Valores fora do limite", "Simulador SIC/XE");
            }

            return long.MaxValue;
        }

        /// <summary>
        /// Escreve um valor na memória a partir da posição especificada, quantidade de bytes "tam". A escrita
        /// é realizada a partir do MSB, ou seja, a posição especificada terá o MSB
        /// </summary>
        /// <param name="posD">Posição inicial de escrita na memória</param>
        /// <param name="tam">Quantidade de bytes que deverão ser escritos</param>
        /// <param name="valor">Valor a ser escrito na memória</param>
        /// <returns>Retorna um booleano indicando se a operação foi bem sucedida</returns>
        public bool Write(int pos, int tam, long valor)
        {
            try
            {
                if (pos >= 0 && pos <= TAM_MEM - tam)
                {
                    memoria[pos + tam - 1] = (byte)(valor & 0xFF);

                    for (int i = tam - 2; i >= 0; i--)
                    {
                        valor = valor >> 8;
                        memoria[pos + i] = (byte)(valor & 0xFF);
                    }

                    AtualizaMem(pos, tam);

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Erro no endereçamento de memória. Valor fora dos limites", "Simulador SIC/XE");
            }

            return false;
        }

        /// <summary>
        /// Lê o valor de chave da posição de memória
        /// </summary>
        /// <param name="posD">posição da memória</param>
        /// <returns>Valor da chave da posição</returns>
        public long ReadKey(int pos)
        {
            if (pos >= 0 && pos <= TAM_MEM)
                return chave[pos];
            else
                return long.MaxValue;
        }

        /// <summary>
        /// Escreve um valor de chave na posição de memória especificada
        /// </summary>
        /// <param name="posD">posição de memória</param>
        /// <param name="valor">Valor da chave a ser escrito</param>
        /// <returns>Retorna um booleano indicando se a operação foi bem sucedida</returns>
        public bool WriteKey(int pos, long valor)
        {
            if (pos >= 0 && pos <= TAM_MEM)
            {
                chave[pos] = (byte)(valor & 0xFF);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Aloca uma porção da memória para carga de programas de acordo com a quantidade de bytes
        /// solicitada. A carga pode ser dinâmica ou estática, ou seja, tanto programas fixos quanto
        /// relocáveis podem ser utilizados. Se o programa for relocável, o parâmetro "posD" retornará
        /// a posição inicial em que ocorreu a alocação de memória
        /// </summary>
        /// <param name="qtdeBytes">Quantidade de bytes contínuos que devem ser alocados</param>
        /// <param name="posD">Posição inicial em que o programa deve ser alocado. Se o programa for relocável
        ///  o parâmetro retornará o endereço inicial da porção de memória alocada</param>
        /// <param name="eRealocavel">Indica se o programa é relocável. Se for relocável "posD" retornará o endereço
        /// inicial da memória alocada, se o programa for fixo então "posD" será usada para verificar espaço na posição especificada</param>
        /// <returns>Retorna "true" se a relocação foi bem sucedida e "false" caso contrário</returns>
        public bool Alloc(int qtdeBytes, ref  uint pos, bool eRealocavel)
        {
            //Verifica se existe espaço para alocação do programa
            if (eRealocavel)
            {
                uint i = 0;
                bool memOk = true;

                do
                {
                    //Encontra o próximo espaço livre na memória
                    //Se não tiver mais memória disponível a alocação não poderá ocorrer
                    while (mapaBits[i] != 0)
                    {
                        i++;

                        if (i == TAM_MEM)
                            return false;
                    }

                    //verifica se tem o tamanho necessário livre
                    for (int j = 0; j < qtdeBytes; j++)
                    {
                        //Se o fim da memória foi encontrado e ainda não há espaço para
                        //carga do programa então a alocação não pode ser realizada
                        if (i + j > TAM_MEM)
                            return false;

                        if (mapaBits[i + j] != 0)
                        {
                            memOk = false;
                            break;
                        }
                    }

                    //Coloca na posição inicial de reserva o endereço inicial da alocação encontrado
                    pos = i;

                } while (!memOk);
            }
            else
            {
                //Se não é relocável, então a memória está livre?
                if (mapaBits[pos] == 0)
                {
                    for (int i = 0; i < qtdeBytes; i++)
                        if (mapaBits[pos + i] != 0) return false;
                }
                else
                    return false;
            }

            //Como tem espaço na memória pro programa, reserve o espaço
            for (int i = 0; i < qtdeBytes; i++)
                mapaBits[pos + i] = 1;

            return true;
        }

        /// <summary>
        /// Desaloca uma determinada posição de memória, liberando espaço uso
        /// </summary>
        /// <param name="posD">Endereço inicial de desalocação de memória</param>
        /// <param name="qtdeBytes">Quantidade de bytes que devem ser desalocados</param>
        /// <returns>Retorna "true" se a desalocação for bem sucedida e "false" caso contrário</returns>
        public bool Dealloc(uint pos, int qtdeBytes)
        {
            //Reset na memória
            if (qtdeBytes == -1)
            {
                for (int i = 0; i < TAM_MEM; i++)
                    mapaBits[i] = 0;
				
				return true;
            }
            else if (pos + qtdeBytes <= TAM_MEM)
            {
                for (int i = 0; i < qtdeBytes; i++)
                    mapaBits[pos + i] = 0;

                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Evento de atualização da memória. Utilizado pelo simulador para atualizar só a parte atualizada
        /// da memória e não ter que atualizar todo o conteúdo carregado. Utilizada em conjunto com a função Write.
        /// </summary>
        /// <param name="pos">Posição em que a memória foi atualizada</param>
        /// <param name="qtde">quantidade de bytes atualizada da memória</param>
        protected void AtualizaMem(int pos, int qtde)
        {
            if (PosModif != null)
            {
                PosModif(pos, qtde);
            }
        }

        public byte this[int pos]
        {
            get
            {
                if (pos < 0 || pos > TAM_MEM)
                {
                    System.Windows.MessageBox.Show("Erro no endereçamento de memória. Valor fora dos limites", "Simulador SIC/XE");
                    return byte.MaxValue;
                }
                else
                    return memoria[pos];
            }
        }
    }
}

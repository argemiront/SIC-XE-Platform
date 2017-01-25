using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Text;
using System.IO;

namespace Simulador
{
    class ControladorES
    {
        HashSet<byte> dispositivos;
        System.IO.StringReader texto;

        //Construtor Default
        public ControladorES()
        {
            dispositivos = new HashSet<byte>();
        }

        /// <summary>
        /// Cria um novo controlador de E/S.
        /// </summary>
        /// <param name="texto">String que servirá como dados de entrada de um dispositivo qualquer</param>
        public ControladorES(string texto)
        {
            dispositivos = new HashSet<byte>();
            this.texto = new System.IO.StringReader(texto);
        }

        /// <summary>
        /// Lê o valor da entrada de um dispositivo. Na prática, está lendo um caractere do string enviado na
        /// construção.
        /// </summary>
        /// <param name="endereco">Endereço do dispositivo de IO</param>
        /// <returns>Retorna o valor armanzenado no buffer do dispositivo. No caso, a string de construção</returns>
        public byte ReadIO(byte endereco)
        {
            if (dispositivos.Contains(endereco) && texto != null)
            {
                return (byte)texto.Read();
            }

            return byte.MaxValue;
        }

        public bool WriteIO(byte endereco, byte valor)
        {
            if (dispositivos.Contains(endereco))
            {
                string msg = "Escrita no endereço " + endereco.ToString("X") + ", o valor: " + valor.ToString("X");
                System.Windows.MessageBox.Show(msg, "Escrita no IO", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return true;
            }
            else
                return false;
        }

        public bool RegistreIO(byte endereco)
        {
            if (dispositivos.Contains(endereco))
                return false;
            else
                dispositivos.Add(endereco);

            return true;
        }

        public void SetTexto(string texto)
        {
            this.texto = new System.IO.StringReader(texto);
        }
    }
}

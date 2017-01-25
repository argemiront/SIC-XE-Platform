using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Simulador
{
    struct Word
    {
        int valor;

        public const int MaxValue = 16777215;
        public const int MinValue = 0;

        public static long Convert12BitsSigned(long valor)
        {
            //Se o valor é maior que 2047 então é negativo
            if (valor > 2047)
            {
                valor = ~valor + 1;
                valor = valor & 0xFFF;
                valor = -valor;
            }

            return valor;
        }

        //TODO: Verificar como é a leitura e implementar a conversão
        public static long ByteToInt(byte[] bytes)
        {
            long temp = 0;

            for (int i = 0; i < bytes.Length; i++)
            {
                //temp += Int64.Parse(bytes[i].ToString(), System.Globalization.NumberStyles.AllowHexSpecifier);
                temp += bytes[i];
                temp = temp << 8;
            }

            temp = temp >> 8;
            return temp;
        }

        //TODO: Verificar como é a leitura e implementar a conversão
        public static string ByteToString(byte[] bytes)
        {
            string temp = "";

            for (int i = 0; i < bytes.Length; i++)
            {
                temp += Convert.ToChar(bytes[i]).ToString();
            }

            return temp;
        }
    }
}

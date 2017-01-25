using System;
using System.Windows;
using System.Text.RegularExpressions;

namespace Simulador
{
    class SHIFTR : SetInstrucoes
    {
        public SHIFTR()
        {
            base.tamanho = 2;
            base.codigo = 0xA8;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                int reg = (int)((ula.parametroCarregado & 0xF0) >> 2);
                long reg1 = ula.registradores[reg];
                long nbits = ula.parametroCarregado & 0x0F;

                long qtdeBits = Convert.ToInt64(System.Math.Ceiling(System.Math.Log(reg1, 2)));

                for (long i = nbits; i > 0; i--)
                {
                    bool bit = false;

                    var teste1 = reg1 & 1;
                    if (teste1 == 1)
                        bit = true;

                    reg1 = reg1 >> 1;

                    if (bit)
                    {
                        reg1 = reg1 | 8388608;
                        reg1 = reg1 & 16777215;
                    }
                }

                ula.registradores[reg] = reg1;
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    string opcode = "A8";

        //    return base.OPCodeINST2(sic, opcode);
        //}

        public override string ToString()
        {
            return "SHIFTR";
        }
    }
}
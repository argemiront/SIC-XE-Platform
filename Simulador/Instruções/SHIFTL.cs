using System;
using System.Windows;
using System.Text.RegularExpressions;

namespace Simulador
{
    class SHIFTL : SetInstrucoes
    {
        public SHIFTL()
        {
            base.tamanho = 2;
            base.codigo = 0xA4;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                int reg = (int)((ula.parametroCarregado & 0xF0) >> 2);
                long reg1 = ula.registradores[reg];
                long nbits = ula.parametroCarregado & 0x0F;

                long qtdeBits = Convert.ToInt64(System.Math.Ceiling(System.Math.Log(reg1, 2)));

                if (nbits > 24 - qtdeBits)
                {
                    for (long i = nbits; i > 0; i--)
                    {
                        bool bit = false;

                        if (Convert.ToInt32(System.Math.Ceiling(System.Math.Log(reg1, 2))) == 24)
                            bit = true;

                        reg1 = reg1 << 1;

                        if (bit)
                        {
                            reg1 = reg1 | 1;
                            reg1 = reg1 & 16777215;
                        }
                    }
                }
                else
                    reg1 = reg1 << (int)nbits;

                ula.registradores[reg] = reg1;
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    string opcode = "A4";

        //    return base.OPCodeINST2(sic, opcode);
        //}

        public override string ToString()
        {
            return "SHIFTL";
        }
    }
}
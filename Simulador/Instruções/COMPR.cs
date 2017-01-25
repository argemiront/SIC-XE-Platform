using System;
using System.Windows;
using System.Text.RegularExpressions;
using System.Linq;

namespace Simulador
{
    class COMPR : SetInstrucoes
    {
        public COMPR()
        {
            base.tamanho = 2;
            base.codigo = 0xA0;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                int reg1 = (int)((ula.parametroCarregado & 0xF0) >> 2);
                int reg2 = (int)(ula.parametroCarregado & 0x0F);

                int reg1Value = (int)ula.registradores[reg1];
                int reg2Value = (int)ula.registradores[reg2];

                int result;

                if (reg1Value > reg2Value)
                    result = 1;
                else if (reg1Value < reg2Value)
                    result = -1;
                else
                    result = 0;

                //Atualiza Registrador
                ula.registradores[ula.SW] = result;
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    string opcode = "A0";

        //    return base.OPCodeINST2(sic, opcode);
        //}

        public override string ToString()
        {
            return "COMPR";
        }
    }
}
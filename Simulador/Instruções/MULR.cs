using System;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulador
{
    class MULR : SetInstrucoes
    {
        public MULR()
        {
            base.tamanho = 2;
            base.codigo = 0x98;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                int reg1 = (int)((ula.parametroCarregado & 0xF0) >> 2);
                int reg2 = (int)(ula.parametroCarregado & 0x0F);

                ula.registradores[reg2] *= ula.registradores[reg1];
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    string opcode = "98";

        //    return base.OPCodeINST2(sic, opcode);
        //}

        public override string ToString()
        {
            return "MULR";
        }
    }
}

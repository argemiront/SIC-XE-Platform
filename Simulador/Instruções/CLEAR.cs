using System;
using System.Windows;
using System.Text.RegularExpressions;
using System.Linq;

namespace Simulador
{
    class CLEAR : SetInstrucoes
    {
        public CLEAR()
        {
            base.tamanho = 2;
            base.codigo = 0xB4;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                int reg1 = (int)((ula.parametroCarregado & 0xF0) >> 4);

                ula.registradores[reg1] = 0;
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    string opcode = "B4";

        //    return base.OPCodeINST2(sic, opcode);
        //}

        public override string ToString()
        {
            return "CLEAR";
        }
    }
}
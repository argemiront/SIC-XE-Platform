using System;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulador
{
    class RMO : SetInstrucoes
    {
        public RMO()
        {
            base.tamanho = 2;
            base.codigo = 0xAC;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                int reg1 = (int)((ula.parametroCarregado & 0xF0) >> 2);
                int reg2 = (int)(ula.parametroCarregado & 0x0F);

                ula.registradores[reg2] += ula.memoria.Read((int)ula.registradores[reg1], 1);
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    string opcode = "AC";

        //    return base.OPCodeINST2(sic, opcode);
        //}

        public override string ToString()
        {
            return "RMO";
        }
    }
}
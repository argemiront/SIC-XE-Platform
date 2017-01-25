using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Simulador
{
    class FLOAT : SetInstrucoes
    {
        public FLOAT()
        {
            base.tamanho = 1;
            base.codigo = 0xC0;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                //ula.registradores[ula.F] = 
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    string opcode = "9C";

        //    return base.OPCodeINST2(sic, opcode);
        //}

        public override string ToString()
        {
            return "FLOAT";
        }
    }
}

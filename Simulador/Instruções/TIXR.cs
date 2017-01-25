using System;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulador
{
    class TIXR : SetInstrucoes
    {
        public TIXR()
        {
            base.tamanho = 2;
            base.codigo = 0xB8;
        }
        override public void Execute(ULA ula)
        {
            try
            {
                int reg = (int)((ula.parametroCarregado & 0xF0) >> 4);
                ula.registradores[ula.X]++;

                int result;

                if (ula.registradores[ula.X] > ula.registradores[reg])
                    result = 1;
                else if (ula.registradores[ula.X] < ula.registradores[reg])
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
        //    string opcode = "B8";

        //    return base.OPCodeINST2(sic, opcode);
        //}

        public override string ToString()
        {
            return "TIXR";
        }
    }
}
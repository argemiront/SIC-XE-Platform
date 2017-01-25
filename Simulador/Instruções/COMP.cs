using System;
using System.Windows;
using System.Text.RegularExpressions;
using System.Linq;

namespace Simulador
{
    class COMP : SetInstrucoes
    {
        public COMP()
        {
            base.tamanho = 3;
            base.codigo = 0x28;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                int result;

                if (ula.registradores[ula.A] > ula.parametroCarregado)
                    result = 1;
                else if (ula.registradores[ula.A] < ula.parametroCarregado)
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
        //    int codInst = 0x28;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "COMP";
        }
    }
}
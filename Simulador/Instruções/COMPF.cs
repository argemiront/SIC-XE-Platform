using System;
using System.Windows;
using System.Text.RegularExpressions;
using System.Linq;

namespace Simulador
{
    class COMPF : SetInstrucoes
    {
        public COMPF()
        {
            base.tamanho = 3;
            base.codigo = 0x88;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                int result;

                if (ula.registradores[ula.F] > ula.parametroCarregado)
                    result = 1;
                else if (ula.registradores[ula.F] < ula.parametroCarregado)
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
            return "COMPF";
        }
    }
}
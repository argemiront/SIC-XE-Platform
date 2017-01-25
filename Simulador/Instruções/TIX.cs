using System;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulador
{
    class TIX : SetInstrucoes
    {
        public TIX()
        {
            base.tamanho = 3;
            base.codigo = 0x2C;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                ula.registradores[ula.X]++;

                int result;

                if (ula.registradores[ula.X] > ula.parametroCarregado)
                    result = 1;
                else if (ula.registradores[ula.X] < ula.parametroCarregado)
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
        //    int codInst = 0x2C;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "TIX";
        }
    }
}
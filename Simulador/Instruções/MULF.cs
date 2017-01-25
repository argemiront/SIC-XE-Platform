using System;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulador
{
    class MULF : SetInstrucoes
    {
        public MULF()
        {
            base.tamanho = 3;
            base.codigo = 0x60;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                ula.registradores[ula.F] *= ula.parametroCarregado;
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    int codInst = 0x20;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "MULF";
        }
    }
}

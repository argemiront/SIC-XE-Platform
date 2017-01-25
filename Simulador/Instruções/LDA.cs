using System;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulador
{
    class LDA : SetInstrucoes
    {
        public LDA()
        {
            base.tamanho = 3;
            base.codigo = 0x00;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                ula.registradores[ula.A] = ula.parametroCarregado;
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    int codInst = 0x00;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "LDA";
        }
    }
}
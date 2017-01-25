using System;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulador
{
    class LDS : SetInstrucoes
    {
        public LDS()
        {
            base.tamanho = 3;
            base.codigo = 0x6C;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                ula.registradores[ula.S] = ula.parametroCarregado;
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    int codInst = 0x6C;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "LDS";
        }
    }
}
using System;
using System.Windows;
using System.Text.RegularExpressions;
using System.Linq;

namespace Simulador
{
    class SUB : SetInstrucoes
    {
        public SUB()
        {
            base.tamanho = 3;
            base.codigo = 0x1C;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                ula.registradores[ula.A] -= ula.parametroCarregado;
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    int codInst = 0x1C;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "SUB";
        }
    }
}

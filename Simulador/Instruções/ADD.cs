using System;
using System.Windows;
using System.Text.RegularExpressions;
using System.Linq;

namespace Simulador
{
    class ADD : SetInstrucoes
    {
        public ADD()
        {
            base.tamanho = 3;
            base.codigo = 0x18;
        }
        
        override public void Execute(ULA ula)
        {
            try
            {
                ula.registradores[ula.A] += ula.parametroCarregado;
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    int codInst = 0x18;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "ADD";
        }
    }
}

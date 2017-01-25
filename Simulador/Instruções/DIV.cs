using System;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulador
{
    class DIV : SetInstrucoes
    {

        public DIV()
        {
            base.tamanho = 3;
            base.codigo = 0x24;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                ula.registradores[ula.A] /= ula.parametroCarregado;
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    int codInst = 0x24;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "DIV";
        }
    }
}
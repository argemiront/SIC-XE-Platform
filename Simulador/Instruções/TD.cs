using System;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulador
{
    class TD : SetInstrucoes
    {
        public TD()
        {
            base.tamanho = 3;
            base.codigo = 0xE0;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                //TODO: implementar o controlador de IO para ser usado nessa instrução
                ula.registradores[ula.SW] = -1;
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    int codInst = 0xE0;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "TD";
        }
    }
}
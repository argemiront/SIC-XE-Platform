using System;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulador
{
    class JEQ : SetInstrucoes
    {
        public JEQ()
        {
            base.tamanho = 3;
            base.codigo = 0x30;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                if (ula.registradores[ula.SW] == 0)
                    ula.registradores[ula.PC] = ula.enderecoCarregado;
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    int codInst = 0x30;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "JEQ";
        }
    }
}
using System;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulador
{
    class JLT : SetInstrucoes
    {
        public JLT()
        {
            base.tamanho = 3;
            base.codigo = 0x38;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                if (ula.registradores[ula.SW] == -1)
                    ula.registradores[ula.PC] = ula.enderecoCarregado;
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    int codInst = 0x38;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "JLT";
        }
    }
}
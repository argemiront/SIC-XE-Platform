using System;
using System.Windows;
using System.Linq;
using System.Text.RegularExpressions;

namespace Simulador
{
    class JSUB : SetInstrucoes
    {

        public JSUB()
        {
            base.tamanho = 3;
            base.codigo = 0x48;
        }

        override public void Execute(ULA ula)
        {
            try
            {
                ula.registradores[ula.L] = ula.registradores[ula.PC];
                ula.registradores[ula.PC] = ula.enderecoCarregado;
            }
            catch (Exception ex)
            {
            }
        }

        //override public string OPCode(SIC sic)
        //{
        //    int codInst = 0x48;

        //    return base.OPCodeINST3(sic, codInst);
        //}

        public override string ToString()
        {
            return "JSUB";
        }
    }
}
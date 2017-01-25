using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Simulador
{
    abstract class SetInstrucoes
    {
        protected int tamanho;
        protected int codigo;
        public int Tamanho { get { return tamanho; } }
        public int Codigo { get { return codigo; } }

        public abstract void Execute(ULA ula);
        //public abstract string OPCode(SIC sic);

        //protected string OPCodeINST1(SIC sic, string opcode)
        //{
        //    return "";
        //}

        //protected string OPCodeINST2(SIC sic, string opcode)
        //{
        //    int codInst;
        //    string OPMontada = opcode;

        //    string strINST = @"\s*(?<label>\S+)?\s+((?<inst>" + Resource1.STR_INSTRUCOES + @"))\s+(?<param1>[^,\s]+)(,(?<param2>([^,\s]+|\d+)))?";
        //    Match matchInst = Regex.Match(sic.LinhaInstMont, strINST);

        //    var label = matchInst.Groups["label"].Value;
        //    var param1 = matchInst.Groups["param1"].Value;
        //    var param2 = matchInst.Groups["param2"].Value;

        //    string zero = "000000";

        //    if (param2 == "")
        //    {
        //        sic.SYMTAB.TryGetValue(param1, out codInst);

        //        string OPCodeHexa = String.Format("{0:X}", codInst);
        //        //OPMontada += (OPCodeHexa.Length < 2) ? (zero.Substring(0, 2 - OPCodeHexa.Length) + OPCodeHexa) : OPCodeHexa;
        //        //Retirado para qdo for 1 param ele ficar na esquerda
        //        OPMontada += OPCodeHexa + "0";
        //    }
        //    else
        //    {
        //        int reg1, reg2;

        //        sic.SYMTAB.TryGetValue(matchInst.Groups["param1"].Value, out reg1);

        //        if (Regex.IsMatch(param2, @"\d+"))
        //            Int32.TryParse(param2, out reg2);
        //        else
        //            sic.SYMTAB.TryGetValue(param2, out reg2);

        //        string reg1Hexa = String.Format("{0:X}", reg1);
        //        string reg2Hexa = String.Format("{0:X}", reg2);

        //        OPMontada += reg1Hexa + reg2Hexa;
        //    }

        //    return OPMontada;
        //}

        //protected string OPCodeINST3(SIC sic, int codInst)
        //{
        //    string OPMontada = "";
        //    int tamInstLocal = 3;
        //    string strINST = @"\s*(?<label>\S+)?\s+(?<tamES>\+)?((?<inst>" + Resource1.STR_INSTRUCOES + @"))\s+(?<param>(\S+\s*[^\r\n\t])*)?";
        //    Match matchInst = Regex.Match(sic.LinhaInstMont, strINST);

        //    var label = matchInst.Groups["label"].Value;
        //    var tamES = matchInst.Groups["tamES"].Value;
        //    var inst = matchInst.Groups["inst"].Value;
        //    var param = matchInst.Groups["param"].Value;

        //    string zero = "000000";

        //    if (tamES == "+")
        //        tamInstLocal++;

        //    if (param.StartsWith("#"))    //endereçamento imediato
        //    {
        //        //variável qualquer para receber o valor da SYMTAB...
        //        int descartavel;


        //        //N = 0, I = 1
        //        codInst += 1;

        //        //XBPE = 0000
        //        codInst = codInst << 4;

        //        //Se tamanho = 4: E = 1
        //        if (tamInstLocal == 4)
        //            codInst++;
        //        else if (sic.SYMTAB.TryGetValue(param.Substring(1, param.Length - 1), out descartavel))
        //        {
        //            //Se estamos tratando com variáveis, então:
        //            //Verifica se é relativo à BASE ou PC
        //            if (sic.BASE)
        //                codInst += 4;
        //            else
        //                codInst += 2;
        //        }

        //        string OPCodeHexa = String.Format("{0:X}", codInst);
        //        OPMontada += (OPCodeHexa.Length < 3) ? (zero.Substring(0, 3 - OPCodeHexa.Length) + OPCodeHexa) : OPCodeHexa;
        //        codInst = 0;

        //        if (!sic.SYMTAB.TryGetValue(param.Substring(1, param.Length - 1), out codInst))
        //            codInst = Int32.Parse(param.Substring(1)/*, System.Globalization.NumberStyles.AllowHexSpecifier*/);
        //        else
        //            codInst -= sic.LOCCTR;

        //        string OPEndHexa = String.Format("{0:X}", codInst);

        //        if (tamInstLocal == 3)
        //            OPMontada += (OPEndHexa.Length < 3) ? (zero.Substring(0, 3 - OPEndHexa.Length) + OPEndHexa) : OPEndHexa;
        //        else
        //            OPMontada += (OPEndHexa.Length < 5) ? (zero.Substring(0, 5 - OPEndHexa.Length) + OPEndHexa) : OPEndHexa;
        //    }
        //    else if (param.StartsWith("="))
        //    {
        //        //Match matchLit = Regex.Match(param, @"=(X|)");
        //    }
        //    else if (param.StartsWith("@"))   //endereçamento indireto
        //    {
        //        //Adiciona N=1 e I=0
        //        codInst += 2;

        //        //XBPE 0000
        //        codInst = codInst << 4;

        //        //Se tamanho = 4: E = 1
        //        if (tamInstLocal == 4)
        //            codInst++;
        //        //Verifica se é relativo à BASE ou PC
        //        else if (sic.BASE)
        //            codInst += 4;
        //        else
        //            codInst += 2;

        //        //Armazena os 3 primeiros caracteres em hexa
        //        string OPCodeHexa = String.Format("{0:X}", codInst);
        //        OPMontada += (OPCodeHexa.Length < 3) ? (zero.Substring(0, 3 - OPCodeHexa.Length) + OPCodeHexa) : OPCodeHexa;
        //        codInst = 0;


        //        //Verifica o endereço de memória
        //        int posMemProg;
        //        string var = param.Substring(1); //remove o @

        //        if (sic.EXTREF.ContainsKey(var))    //Se referência externa -> end. = 0
        //            codInst = 0;
        //        else if (tamInstLocal == 4 && sic.SYMTAB.TryGetValue(var, out posMemProg)) //Tamanho extendido pega o end. diretamente
        //        {
        //            codInst = posMemProg;
        //        }
        //        else if (sic.SYMTAB.TryGetValue(var, out posMemProg))  //Se é uma posição de memória
        //        {
        //            if (sic.BASE)
        //            {
        //                codInst = posMemProg /*- sic.SYMTAB["B"]*/;
        //            }
        //            else
        //            {
        //                codInst = posMemProg - sic.LOCCTR;
        //            }
        //        }
        //        else
        //        {
        //            //Erro no programa
        //        }

        //        //Armazena o valor de codInt
        //        string OPEndHexa = String.Format("{0:X}", codInst);

        //        if (tamInstLocal == 3)
        //            OPMontada += (OPEndHexa.Length < 3) ? (zero.Substring(0, 3 - OPEndHexa.Length) + OPEndHexa) : OPEndHexa;
        //        else
        //            OPMontada += (OPEndHexa.Length < 5) ? (zero.Substring(0, 5 - OPEndHexa.Length) + OPEndHexa) : OPEndHexa;
        //    }
        //    else    //endereço de memória
        //    {
        //        bool indexado = false;
        //        bool peloPC = false;
        //        bool extendido = false;

        //        if (!sic.sicCompativel)
        //            codInst += 3;   //Adiciona N=1 e I=1

        //        //XBPE
        //        codInst = codInst << 4;

        //        //Verifica se é indexado
        //        if (param.EndsWith(",X"))
        //        {
        //            indexado = true;
        //            codInst += 8;
        //        }

        //        //Se tamanho = 4: E = 1
        //        if (tamInstLocal == 4)
        //        {
        //            extendido = true;
        //            codInst++;
        //        }
        //        //Verifica se é relativo à BASE ou PC
        //        else if (sic.BASE)
        //        {
        //            peloPC = false;
        //            codInst += 4;
        //        }
        //        else
        //        {
        //            peloPC = true;
        //            codInst += 2;
        //        }

        //        //Armazena os 3 primeiros caracteres em hexa
        //        string OPCodeHexa = String.Format("{0:X}", codInst);
        //        OPMontada += (OPCodeHexa.Length < 3) ? (zero.Substring(0, 3 - OPCodeHexa.Length) + OPCodeHexa) : OPCodeHexa;
        //        codInst = 0;
        //        //Verifica o endereço de memória
        //        int posMemProg;
        //        string endMem = param;
        //        endMem = (indexado) ? endMem.Remove(endMem.Length - 2) : endMem;    //Remove o ",X" do final

        //        //Monta o endereço da memória e registros de modificação
        //        if (sic.EXTREF.ContainsKey(endMem))
        //        {
        //            //Registro de modificação
        //            var locctrHexa = String.Format("{0:X}", sic.LOCCTR + 1);
        //            locctrHexa = (locctrHexa.Length < 6) ? (zero.Substring(0, 6 - locctrHexa.Length) + locctrHexa) : locctrHexa;
        //            string qtdeMeioBit = (extendido)? "05" : "03";
        //            sic.modificadores.Add("M" + locctrHexa + qtdeMeioBit + "+" + endMem);
        //        }
        //        else if (sic.SYMTAB.TryGetValue(endMem, out posMemProg))
        //        {
        //            if (peloPC)
        //                codInst = posMemProg - sic.LOCCTR;
        //            else
        //                codInst = posMemProg /*- sic.SYB["B"]*/;

        //            //Registro de modificação
        //            var locctrHexa = String.Format("{0:X}", sic.LOCCTR + 1);
        //            locctrHexa = (locctrHexa.Length < 6) ? (zero.Substring(0, 6 - locctrHexa.Length) + locctrHexa) : locctrHexa;
        //            string qtdeMeioBit = (extendido) ? "05" : "03";
        //            sic.modificadores.Add("M" + locctrHexa + qtdeMeioBit); 
        //        }


        //        //Armazena o valor de codInt
        //        string OPEndHexa = String.Format("{0:X}", codInst);

        //        if (!extendido)
        //            OPMontada += (OPEndHexa.Length <= 3) ? (zero.Substring(0, 3 - OPEndHexa.Length) + OPEndHexa) : OPEndHexa.Substring(OPEndHexa.Length - 4);
        //        else
        //            OPMontada += (OPEndHexa.Length <= 5) ? (zero.Substring(0, 5 - OPEndHexa.Length) + OPEndHexa) : OPEndHexa.Substring(OPEndHexa.Length - 6);
        //    }

        //    return OPMontada;
        //}
    }
}

using System;
using System.Collections.Generic;
using System.IO;

public class Escalonador
{
    private readonly string inputPath;
    private readonly string outputPath;
    private List<string> objetos = new();
    private Dictionary<string, int> timestamps = new();
    private string[] linhasEscalonamentos = Array.Empty<string>();

    public Escalonador(string inputPath, string outputPath)
    {
        this.inputPath = inputPath;
        this.outputPath = outputPath;
    }

    public void Executar()
    {
        var linhas = File.ReadAllLines(inputPath);
        objetos.AddRange(linhas[0].TrimEnd(';').Split(", "));

        var transacoes = linhas[1].TrimEnd(';').Split(", ");
        var ts = linhas[2].TrimEnd(';').Split(", ");
        for (int i = 0; i < transacoes.Length; i++)
            timestamps[transacoes[i]] = int.Parse(ts[i]);

        linhasEscalonamentos = linhas[3..];

        using StreamWriter outWriter = new StreamWriter(outputPath);

        foreach (var linha in linhasEscalonamentos)
        {
            string resultado = ProcessarEscalonamento(linha.Trim());
            outWriter.WriteLine(resultado);
        }
    }

    private string ProcessarEscalonamento(string linha)
    {
        string idEscalonamento = linha.Split('-')[0];
        string[] operacoes = linha[(idEscalonamento.Length + 1)..].Trim().Split(' ');
        int momento = 0;
        int commit = 0;

        var estadoObjetos = new Dictionary<string, DadosObjeto>();
        foreach (var nome in objetos)
        {
            var dado = new DadosObjeto(nome);
            estadoObjetos[nome] = dado;
        }

        var objetosUsadosPorTransacao = new Dictionary<string, HashSet<string>>();
        var transacaoPorDado = new Dictionary<string, string>();

        foreach (string op in operacoes)
        {
            if (string.IsNullOrWhiteSpace(op)) continue;

            if (op.StartsWith("c"))
            {
                commit++;
                foreach (var dado in estadoObjetos.Values)
                {
                    dado.TS_Read = 0;
                    dado.TS_Write = 0;
                }
                continue;
            }

            if (op.Length < 5 || op[2] != '(' || !op.EndsWith(")"))
                continue;

            char tipo = op[0]; // r ou w
            string tid = "t" + op[1]; // t1, t2, etc
            string obj = op[3].ToString(); // X, Y, Z
            int tsT = timestamps.ContainsKey(tid) ? timestamps[tid] : 0;

            if (!objetosUsadosPorTransacao.ContainsKey(tid))
                objetosUsadosPorTransacao[tid] = new HashSet<string>();

            objetosUsadosPorTransacao[tid].Add(obj);
            transacaoPorDado[obj] = tid;

            var estado = estadoObjetos[obj];
            bool rollback = false;

            if (tipo == 'r')
            {
                if (tsT < estado.TS_Write)
                    rollback = true;
                else
                    estado.TS_Read = Math.Max(estado.TS_Read, tsT);
            }
            else if (tipo == 'w')
            {
                if (tsT < estado.TS_Read || tsT < estado.TS_Write)
                    rollback = true;
                else
                    estado.TS_Write = tsT;
            }

            string operacaoTexto = tipo == 'r' ? "Read" : "Write";

            if (rollback)
            {
                int momentoRollback = momento + commit;
                File.AppendAllText("logs/log_roulback.txt", $"{idEscalonamento},{operacaoTexto.ToLower()},{momentoRollback}\n");
                return $"{idEscalonamento}-ROLLBACK-{momentoRollback}";
            }
                

            momento++;

            if (momento != 0)
            {
                estado.SalvarLog(idEscalonamento, operacaoTexto, momento);
            }
        }

        return $"{idEscalonamento}-OK";
    }
}

using System;
using System.IO;
using System.Text;

class Maze
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== GENERATORE DI LABIRINTI ===");
        Console.WriteLine();

        for (int i = 1; i <= 8; i++)
        {
            Console.WriteLine($"Generando labirinto {i}/8...");

            // Crea un generatore con seed diverso per ogni labirinto
            MazeGenerator generator = Crea_Labirinto_Mascherato(i * 1000);

            // Ottieni la posizione di partenza e genera ricorsivamente il labirinto
            MazeGenerator.Coordinate startPos = generator.Starting_Point();
            generator.Generatore_Labirinti(startPos);

            MazeGenerator.Cell[,] maze_aggiornato = new MazeGenerator.Cell[generator.size * 2, generator.size * 2];
            maze_aggiornato = generator.ExpandMatrix(generator.GetMaze());
            
            // Esporta con nome univoco
            string nomeFile = $"labirinto_{i:D2}";
            MazeExporter.EsportaTXT(maze_aggiornato, generator.size * 2, $"{nomeFile}.txt");
            //MazeExporter.EsportaCSV(generator, $"{nomeFile}.csv");
            
            Console.WriteLine($"Labirinto {i} completato!");
        }
        
        // Stampa il labirinto
        //StampaLabirinto(generator);

        Console.WriteLine("Premi un tasto per uscire...");
        Console.ReadKey();
    }
    
    static void StampaLabirinto(MazeGenerator generator)
    {
        // Ottieni il labirinto usando il metodo pubblico GetMaze()
        MazeGenerator.Cell[,] maze = generator.GetMaze();
        
        for (int i = 0; i < generator.size; i++)
        {
            for (int j = 0; j < generator.size; j++)
            {
                if (maze[i, j].value == 0)
                    Console.Write("█"); // Muro (carattere block)
                else if (maze[i, j].value == 1)
                    Console.Write(" "); // Corridoio (spazio)
                else
                    Console.Write("~"); // Maschera
            }
            Console.WriteLine(); // Nuova riga alla fine di ogni riga del labirinto
        }
    }

    static MazeGenerator Crea_Labirinto_Mascherato(int seed = -1)
    {
        int size = 155; //dimensione 77x77

        MazeGenerator generator;
        if (seed != -1)
        {
            generator = new MazeGenerator(size, seed);
        }
        else
        {
            generator = new MazeGenerator(size);
        }

        generator.Inizializza_Labirinto();
        generator.Crea_Maschera_Anulare();
        return generator;
    }

}

// Includi qui la classe MazeGenerator completa
public class MazeGenerator
{
    public int size;
    Random random;
    Cell[,] maze;

    public struct Cell
    {
        public int value;       // 0 = muro, 1 = percorso
        public bool visited;    // per l'algoritmo di generazione
    }

    public struct Coordinate
    {
        public int x;           // colonne
        public int y;           // righe
    }

    public MazeGenerator(int size, int seed = -1)
    {
        this.size = size;
        maze = new Cell[size, size];

        if (seed != -1)
        {
            random = new Random(seed);
        }
        else
        {
            random = new Random();
        }
    }

    public Cell[,] GetMaze()
    {
        return maze;
    }

    public void Inizializza_Labirinto()
    {
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                maze[i, j].visited = false;
                maze[i, j].value = (i % 2 == 1 && j % 2 == 1) ? 1 : 0;
            }
        }
    }

    public Coordinate Starting_Point()
    {
        Coordinate index;
        index.y = 1;
        index.x = size / 2;
        maze[index.y, index.x].visited = true;
        return index;
    }

    public void Generatore_Labirinti(Coordinate index)
    {
        Generatore_Ricorsivo(index);
    }

    public void Generatore_Ricorsivo(Coordinate index)
    {
        //Controllo il numero di vicini non visitati
        Coordinate[] vicini = new Coordinate[4];
        int indice_vicini = 0; // numero di vicini disponibili
        //vicino superiore
        if (index.y > 1)
        {
            Coordinate su = new Coordinate();
            su.y = index.y - 2;
            su.x = index.x;
            if (!maze[su.y, su.x].visited)
            {
                vicini[indice_vicini] = su;
                indice_vicini++;
            }
        }
        //vicino inferiore
        if (index.y < size - 2)
        {
            Coordinate giu = new Coordinate();
            giu.y = index.y + 2;
            giu.x = index.x;
            if (!maze[giu.y, giu.x].visited)
            {
                vicini[indice_vicini] = giu;
                indice_vicini++;
            }
        }
        //vicino sinistro
        if (index.x > 1)
        {
            Coordinate sx = new Coordinate();
            sx.y = index.y;
            sx.x = index.x - 2;
            if (!maze[sx.y, sx.x].visited)
            {
                vicini[indice_vicini] = sx;
                indice_vicini++;
            }
        }
        //vicino destro
        if (index.x < size - 2)
        {
            Coordinate dx = new Coordinate();
            dx.y = index.y;
            dx.x = index.x + 2;
            if (!maze[dx.y, dx.x].visited)
            {
                vicini[indice_vicini] = dx;
                indice_vicini++;
            }
        }

        // CASO BASE: NESSUN VICINO DISPONIBILE
        if (indice_vicini == 0) return;


        // PASSO RICORSIVO
        while (indice_vicini > 0)
        {
            Coordinate prossimo;
            int next = random.Next(0, indice_vicini);
            prossimo = vicini[next];
            maze[prossimo.y, prossimo.x].visited = true;
            // devo "rompere" il muro che li separa
            Coordinate ind_muro;
            // caso 1: next su/giu
            if (index.y - prossimo.y == 0)
            {
                ind_muro.y = index.y;
                ind_muro.x = (index.x + prossimo.x) / 2;
                maze[ind_muro.y, ind_muro.x].value = 1;
                maze[ind_muro.y, ind_muro.x].visited = true;
            }
            // caso 2: next sx/dx
            else if (index.x - prossimo.x == 0)
            {
                ind_muro.y = (index.y + prossimo.y) / 2;
                ind_muro.x = index.x;
                maze[ind_muro.y, ind_muro.x].value = 1;
                maze[ind_muro.y, ind_muro.x].visited = true;
            }

            Generatore_Ricorsivo(prossimo);

            // Rimuovo il vicino appena visitato
            for (int i = next; i < indice_vicini - 1; i++)
            {
                vicini[i] = vicini[i + 1];
            }
            indice_vicini--;

            // Controllo se i vicini rimasti sono ancora disponibili (visited = false)
            for (int i = 0; i < indice_vicini; i++)
            {
                if (maze[vicini[i].y, vicini[i].x].visited)
                {
                    // Rimuovo il vicino non più disponibile
                    for (int j = i; j < indice_vicini - 1; j++)
                    {
                        vicini[j] = vicini[j + 1];
                    }
                    indice_vicini--;
                    i--; // Decremento i per controllare nuovamente la stessa posizione
                }
            }
        }
    }

    public void Crea_Maschera_Anulare()
    {
        double raggio = size / 2.0; // Raggio = metà della dimensione (38.5)
        int centro = size / 2; // Centro della matrice (38)

        // Imposta a 2 tutti i punti esterni al cerchio
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                // Se il punto è FUORI dal cerchio, impostalo a 2
                if (!puntoInCerchio(j, i, raggio, centro, centro))
                {
                    maze[i, j].value = 2;
                    maze[i, j].visited = true;
                }
                // I punti DENTRO il cerchio rimangono a 0 (invariati)
            }
        }

        // Imposta a 2 tutti i punti della zona centrale del cerchio
        int dim_zona_centrale = size / 3;
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (i > (dim_zona_centrale + 15) && i < ((dim_zona_centrale * 2) - 14)
                    && j > (dim_zona_centrale + 15) && j < ((dim_zona_centrale * 2) - 14))
                {
                    maze[i, j].value = 2;
                    maze[i, j].visited = true;
                }
            }
        }

        // Elimino i corridoi adiacenti alla maschera
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (maze[i, j].value == 1)
                {
                    if (Vicino_Mascherato(i, j))
                    {
                        maze[i, j].value = 0;
                    }
                }
            }
        }

        // Flaggo tutte le caselle diverse dai corridoi
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (maze[i, j].value != 1)
                {
                    maze[i, j].visited = true;
                }
            }
        }
    }

    public bool puntoInCerchio(int x, int y, double raggio, int centroX, int centroY)
    {
        // Calcola la distanza dal centro del cerchio
        double distanza = Math.Sqrt((x - centroX) * (x - centroX) + (y - centroY) * (y - centroY));
        return distanza <= raggio;
    }

    public bool Vicino_Mascherato(int riga, int colonna)
    {
        // Definisce le 8 direzioni: N, NE, E, SE, S, SW, W, NW
        int[] deltaX = { 0, 1, 1, 1, 0, -1, -1, -1 };
        int[] deltaY = { -1, -1, 0, 1, 1, 1, 0, -1 };

        for (int i = 0; i < 8; i++)
        {
            int nuovoX = colonna + deltaX[i];
            int nuovoY = riga + deltaY[i];

            // Verifica che il punto sia dentro i limiti della matrice
            if (nuovoX >= 0 && nuovoX < size && nuovoY >= 0 && nuovoY < size)
            {
                if (maze[nuovoY, nuovoX].value == 2) return true;
            }
        }
        return false;
    }
    
    public Cell[,] ExpandMatrix(Cell[,] input)
    {
        Cell[,] output = new Cell[2 * size, 2 * size];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                int value = input[i, j].value;

                // espandi in 2x2
                output[2 * i,     2 * j].value     = value;
                output[2 * i,     2 * j + 1].value = value;
                output[2 * i + 1, 2 * j].value     = value;
                output[2 * i + 1, 2 * j + 1].value = value;
            }
        }

        return output;
    }
}

public class MazeExporter
{
    // METODO 1: Esporta come file CSV
    public static void EsportaCSV(MazeGenerator generator, string percorso = "labirinto.csv")
    {
        MazeGenerator.Cell[,] maze = generator.GetMaze();
        StringBuilder csv = new StringBuilder();
        
        for (int i = 0; i < generator.size; i++)
        {
            for (int j = 0; j < generator.size; j++)
            {
                csv.Append(maze[i, j].value);
                if (j < generator.size - 1)
                    csv.Append(",");
            }
            csv.AppendLine();
        }
        
        File.WriteAllText(percorso, csv.ToString());
        Console.WriteLine($"Labirinto esportato in: {percorso}");
    }

    // METODO 2: Esporta come file di testo semplice
    public static void EsportaTXT(MazeGenerator.Cell[,] maze, int size, string percorso = "labirinto.txt")
    {
        StringBuilder txt = new StringBuilder();

        // Prima riga: dimensioni
        txt.AppendLine(size.ToString());
        
        // Dati della matrice
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                txt.Append(maze[i, j].value);
            }
            txt.AppendLine();
        }
        
        File.WriteAllText(percorso, txt.ToString());
        Console.WriteLine($"Labirinto esportato in: {percorso}");
    }

    // METODO 3: Esporta come array C# per Unity
    public static void EsportaCSharpArray(MazeGenerator generator, string percorso = "LabirintoData.cs")
    {
        MazeGenerator.Cell[,] maze = generator.GetMaze();
        StringBuilder code = new StringBuilder();
        
        code.AppendLine("using UnityEngine;");
        code.AppendLine();
        code.AppendLine("public static class LabirintoData");
        code.AppendLine("{");
        code.AppendLine($"    public const int SIZE = {generator.size};");
        code.AppendLine();
        code.AppendLine("    public static readonly int[,] MAZE_DATA = new int[,]");
        code.AppendLine("    {");
        
        for (int i = 0; i < generator.size; i++)
        {
            code.Append("        { ");
            for (int j = 0; j < generator.size; j++)
            {
                code.Append(maze[i, j].value);
                if (j < generator.size - 1)
                    code.Append(", ");
            }
            code.Append(" }");
            if (i < generator.size - 1)
                code.Append(",");
            code.AppendLine();
        }
        
        code.AppendLine("    };");
        code.AppendLine("}");
        
        File.WriteAllText(percorso, code.ToString());
        Console.WriteLine($"Classe C# esportata in: {percorso}");
    }

    // METODO 4: Esporta come JSON
    public static void EsportaJSON(MazeGenerator generator, string percorso = "labirinto.json")
    {
        MazeGenerator.Cell[,] maze = generator.GetMaze();
        StringBuilder json = new StringBuilder();
        
        json.AppendLine("{");
        json.AppendLine($"  \"size\": {generator.size},");
        json.AppendLine("  \"data\": [");
        
        for (int i = 0; i < generator.size; i++)
        {
            json.Append("    [");
            for (int j = 0; j < generator.size; j++)
            {
                json.Append(maze[i, j].value);
                if (j < generator.size - 1)
                    json.Append(", ");
            }
            json.Append("]");
            if (i < generator.size - 1)
                json.Append(",");
            json.AppendLine();
        }
        
        json.AppendLine("  ]");
        json.AppendLine("}");
        
        File.WriteAllText(percorso, json.ToString());
        Console.WriteLine($"JSON esportato in: {percorso}");
    }
}
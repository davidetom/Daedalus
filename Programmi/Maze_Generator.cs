using System;

class Maze
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== GENERATORE DI LABIRINTI ===");
        Console.WriteLine();
        
        // Chiedi all'utente la dimensione desiderata
        int size = ChiediDimensione();
        
        // Crea un'istanza del generatore di labirinti con la dimensione scelta
        MazeGenerator generator = new MazeGenerator(size);
        
        Console.WriteLine($"Dimensione labirinto: {generator.size}x{generator.size}");
        Console.WriteLine();
        
        // Inizializza il labirinto e ottieni la posizione di partenza
        MazeGenerator.Coordinate startPos = generator.Inizializza_Labirinto();
        Console.WriteLine($"Posizione di partenza: ({startPos.x}, {startPos.y})");
        
        // Genera il labirinto usando l'algoritmo ricorsivo
        generator.Generatore_Labirinti(startPos);
        
        // Crea le entrate e uscite
        generator.Crea_Entrate_Uscite();
        
        Console.WriteLine();
        Console.WriteLine("Labirinto generato:");
        Console.WriteLine();
        
        // Stampa il labirinto
        StampaLabirinto(generator);
        
        Console.WriteLine();
        Console.WriteLine("Legenda:");
        Console.WriteLine("█ = Muro");
        Console.WriteLine("  = Corridoio/Percorso");
        Console.WriteLine();
        Console.WriteLine("Premi un tasto per uscire...");
        Console.ReadKey();
    }

    static int ChiediDimensione()
    {
        int size;
        
        while (true)
        {
            Console.Write("Inserisci la dimensione del labirinto (>= 2): ");
            string input = Console.ReadLine();
            
            // Controlla se l'input è un numero valido
            if (int.TryParse(input, out size))
            {
                // Controlla se la dimensione è valida (>= 2)
                if (size >= 2) return size*2 + 1;
                else
                {
                    Console.WriteLine("Errore: La dimensione deve essere almeno 2.");
                }
            }
            else
            {
                Console.WriteLine("Errore: Inserisci un numero valido.");
            }
            Console.WriteLine();
        }
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
                else
                    Console.Write(" "); // Corridoio (spazio)
            }
            Console.WriteLine(); // Nuova riga alla fine di ogni riga del labirinto
        }
    }
}

// Includi qui la classe MazeGenerator completa
public class MazeGenerator
{
    public int size;

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

    Random random = new Random();

    Cell[,] maze;

    public MazeGenerator(int size)
    {
        this.size = size;
        this.maze = new Cell[size, size];
    }
    
    public Cell[,] GetMaze()
    {
        return maze;
    }

    public Coordinate Inizializza_Labirinto()
    {
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                maze[i, j].visited = false;
                maze[i, j].value = (i % 2 == 1 && j % 2 == 1) ? 1 : 0;
            }
        }
        //casella di partenza
        Coordinate index;
        index.y = 1;
        index.x = random.Next(0, size / 2) * 2 + 1;
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

    public void Crea_Entrate_Uscite()
    {
        //casella di partenza
        Coordinate entrata;
        entrata.y = size - 1;
        entrata.x = random.Next(1, size - 1);
        maze[entrata.y, entrata.x].value = 1;

        //casella di arrivo
        Coordinate uscita;
        uscita.y = 0;
        uscita.x = random.Next(1, size - 1);
        maze[uscita.y, uscita.x].value = 1;
    }
}
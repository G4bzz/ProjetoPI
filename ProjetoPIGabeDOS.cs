using System.Drawing;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;

internal class ProjetoPIGabeDOS
{
    //Discente: Gabriel de Oliveira Santos
    //Disciplina: Processamento de Imagens
    //Turma: 01

    static (bool fixImagePixels, bool debugMode, string? path) handleParams(string[] command_line_params)
    {
        (bool fixImagePixels, bool debugMode, string? path) retorno = (false, false, null);

        List<string> commandline_params = new List<string>();
        foreach (var arg in command_line_params) commandline_params.Add(arg);

        if (commandline_params.IndexOf("-fixImagePixels") != -1) retorno.fixImagePixels = true;
        if (commandline_params.IndexOf("-debugMode") != -1) retorno.debugMode = true;
        if (commandline_params.IndexOf("-path") != -1) retorno.path = commandline_params[commandline_params.IndexOf("-path") + 1];
        return retorno;
    }

    static void Main(string[] args)
    {
        (bool fixImagePixels, bool debugMode, string? path) ret = handleParams(args);
        ImageLib ir = new ImageLib(debugMode: ret.debugMode, fixImagePixels: ret.fixImagePixels);
        PBMImage? image = ir.ReadImg(ret.path);
        if(image != null) ir.CountObjectsAndHoles(image);
    }
}

class ImageLib
{
    #region Attributes
    private bool _debugMode = false;
    private bool _fixImagePixels = false;
    #endregion

    #region Methods
    public ImageLib(bool debugMode = false, bool fixImagePixels = false) {
        _debugMode = debugMode;
        _fixImagePixels = fixImagePixels;
    }

    public PBMImage? ReadImg(String? path = null) // Efetua a leitura de uma imagem
    {
        if (path == null)
        {
            Console.WriteLine("Informe o caminho da imagem (com a extensão .pbm no final):");
            path = Console.ReadLine();
        };
        if (!path.Contains(".pbm")) path += (".pbm");

        if (File.Exists(path))
        {   
            int header_lines_count = 0;
            List<string> lines = new List<string>();
            bool is_single_pixel_row = false;
            (int X, int Y) image_size_xy = (0, 0);
            String? current_line;
            StreamReader sr = new StreamReader(path);

            while ((current_line = sr.ReadLine()) != null) // Adiciona as linhas da imagem em uma lista
            {
                if (header_lines_count < 2) // Declaração da imagem e comentário são dispensáveis
                {
                    header_lines_count++;
                    continue;
                }
                if (header_lines_count == 2) // Faz a leitura e armazena o tamanho da imagem
                {
                    String[] size = current_line.Split(' ');
                    image_size_xy = (int.Parse(size[0]), int.Parse(size[1]));
                    header_lines_count++;
                }
                else // Adiciona os pixels na lista
                {
                    // Flag que identifica se o arquivo da imagem está distribuindo um pixel por linha
                    if (current_line.Trim().Length == 1) is_single_pixel_row = true; 
                    lines.Add(current_line);
                }
            }

            if (!is_single_pixel_row) // Se não for um pixel por linha
            {
                if (lines[0].Split(' ').Length > 1) // Caso os pixels estejam separados por um ' '
                {
                    List<string> new_bytes = new List<string>();
                    foreach (var row in lines)
                    {
                        foreach(var pixel in row.Split(' ')) new_bytes.Add(pixel.ToString());
                    }
                    lines = new_bytes;
                }
                else // Caso os pixels estejam todos juntos
                {
                    List<char> bytes = new List<char>(string.Join(' ', lines.ToArray()).Select(x => x).ToList());
                    bytes.RemoveAll(x => x == ' ');
                    List<string> new_bytes = new List<string>();
                    foreach (var pixel in bytes) new_bytes.Add(pixel.ToString());
                    lines = new_bytes;
                }
            }

            // Cria uma matriz com os pixels lidos
            List<List<byte>> matrix = new List<List<byte>>();
            int pixels_count = 0;
            for (int i = 0; i < image_size_xy.X; i++)
            {
                List<byte> row = new List<byte>();
                for (int j = 0; j < image_size_xy.Y; j++)
                {
                    row.Add(byte.Parse(lines[pixels_count]));
                    pixels_count++;
                }
                matrix.Add(row);
            }

            // Instancia a imagem
            PBMImage img = new PBMImage(image_size_xy, matrix);
            if (_fixImagePixels) NegativeImg(img); // Caso a imagem precise ser invertida para que os pixels pretos sejam igual a 1
            return img;
        }
        else
        {
            Console.WriteLine("Caminho inválido, execute o programa novamente.");
        }
        return null;
    }

    private void ExportImg(PBMImage image, string? pathWithName = null) // Salva a imagem em um arquivo. É usado quando o debugMode está ativado.
    {
        List<string> pbm_image = new List<string>();
        pbm_image.Add("P1");
        pbm_image.Add("#Criado por: Gabriel Oliveira.");
        pbm_image.Add($"{image.GetImageSize().Width} {image.GetImageSize().Height}");
        foreach (var row in image.GetImgMatrix())
        {
            pbm_image.Add(string.Join(' ', row.ToArray()));
        }
        if(pathWithName == null)
        {
            Console.WriteLine("Informe o diretório com o nome do arquivo de saída:");
            Console.WriteLine("Exemplo: C:/Users/gabriel/Desktop/imagem1.pbm");
            String? path = Console.ReadLine();
        }

        if (pathWithName != null && pathWithName.Contains(".pbm"))
        {
            using (StreamWriter sw = new StreamWriter(pathWithName))
            {
                foreach (var row in pbm_image) sw.WriteLine(row);
            }
        }
    }

    // Contabiliza e armazena os objetos de uma imagem
    public int CountObjectsInImg(PBMImage image, List<List<Point>> objects_coords)
    {
        // A logica desse método consiste em aplicar uma DFS (Depth-First Search) nos pixels e buscar por objetos compostos por 1's,
        //  utilizando uma matriz do mesmo tamanho da imagem original para sinalizar quais pixels já foram visitados.

        // A DFS avança de acordo com o tipo de movimentação (em cruz) estabelecida na especificação do projeto,
        //  ou seja: (0,1), (0,-1), (1,0) e (-1,0).

        // Além disso, este método junto a DFS além de contabilizar os objetos, armazenam os objetos (lista de pontos) encontrados
        //  em uma lista de objetos, ou seja, uma lista de lista de pontos;

        int objects_count = 0;
        List<List<byte>> visited_matrix = new List<List<byte>>();
        List<Point> single_object = new List<Point> ();

        for(int i = 0; i< image.GetImageSize().Width; i++)
        {
            visited_matrix.Add(Enumerable.Repeat((byte)0, image.GetImageSize().Width).ToList()); 
        }

        for (int i = 0; i < image.GetImageSize().Width; i++)
        {
            for(int j = 0; j < image.GetImageSize().Height; j++)
            {
                if (DfsCountObjects(i, j, image, visited_matrix, single_object))
                {
                    objects_coords.Add(single_object);
                    objects_count++;
                    single_object = new List<Point>();
                }
                else
                {
                    single_object = new List<Point>();
                }
            }
        }
        return objects_count;
    }

    // DFS que contabiliza e identifica  os objetos de uma imagem
    private bool DfsCountObjects(int x, int y, PBMImage image, List<List<byte>> visited_matrix, List<Point> objeto) 
    {
        List<Point> pixel_connections = new List<Point>()
        {
            new Point(1,0), new Point(-1,0), new Point(0,1), new Point(0, -1)
        };
        if ( x >= image.GetImageSize().Width || y >= image.GetImageSize().Height || x  < 0 || y < 0) // Coordenadas fora da imagem
        {
            return false;
        }
        if (visited_matrix[x][y] == 1) // Pixel já visitado
        {
            return false;
        }
        if (image.GetPixel(x, y) == 0) // Fundo da imagem
        {
            return false;
        }
        visited_matrix[x][y] = 1;
        objeto.Add(new Point(x, y));
        foreach (var direction in pixel_connections) // Executa a DFS para cada direção possível
        {
            DfsCountObjects(x + direction.X, y + direction.Y, image, visited_matrix, objeto);
        }

        return true;
    }

    public void CountObjectsAndHoles(PBMImage image) // Método que soluciona o problema proposto no projeto
    {
        List<List<Point>> valid_object_coords = new List<List<Point>>(); // Armazena os objetos válidos
        List<List<Point>> filled_objects_coords = new List<List<Point>>(); // Armazena os objetos preenchidos
        List<List<Point>> holes_coords = new List<List<Point>>(); // Armazena os furos
        PBMImage holes_image = CloneImage(image);

        // Armazena os objetos candidatos (resultantes da diferença entre o obj preenchido com os seus furos)
        List<List<Point>> candidates_coords = new List<List<Point>>();
        
        if(_debugMode == true)
        {
            Console.WriteLine("Imagem de Origem:");
            image.PrintImgMatrix();
        }

        // Contabiliza os objetos válidos da imagem e os armazena na lista valid_object_coords
        int valid_objects_count = CountObjectsInImg(image, valid_object_coords);


        FillHoles(image); // Preenche os furos da imagem (sem verificar se os mesmos são válidos)
        CountObjectsInImg(image, filled_objects_coords);
        
        FloodFillDfs(holes_image);
        // Processa a imagem original fazendo com que ela contenha apenas os furos 
        // Contabiliza os furos da imagem e os armazena na lista holes.coords
        int holes_count = CountObjectsInImg(holes_image, holes_coords);
        
        image.PrintImgMatrix();

        if (_debugMode == true)
        {
            Console.WriteLine("Imagem com os furos (inclusive os inválidos) preenchidos:");
            ExportImg(image, "./debug_imagem_preenchida.pbm");
            image.PrintImgMatrix();
        }

        if (_debugMode == true)
        {
            Console.WriteLine("Furos (inclusive os inválidos) contidos na imagem:");
            ExportImg(holes_image, "./debug_imagem_furos.pbm");
            holes_image.PrintImgMatrix();
        }

        // Lógica responsável por identificar os buracos válidos e contabilizar quantos:
        //  objetos válidos possuem furos
        
        int objects_with_holes_count = 0;

        // Monta uma lista de objetos formados pela diferença entre os objetos preenchidos e os seus furos
        //  no intuito de comparar esses objetos com os objetos válidos e ver se de fato os furos
        //  encontrados são válidos (pertencem a um objeto válido).
        foreach (var single_object in filled_objects_coords) // Objetos preenchidos
        {
            List <Point> holesInObject = new List<Point> (); // Lista de furos de um único objeto
            foreach (var hole in holes_coords) //Furos
            {
                if (single_object.Intersect(hole).Count() > 0 && single_object.Count() > 0) { // Verifica se o furo atual pertence a um objeto
                    holesInObject.AddRange(hole); // Se sim, o adiciona na lista de furos deste objeto
                }
            }
            // single_object.Count() > 3: objeto que pode conter um furo
            if (single_object.Count() > 3 && holesInObject.Count() > 0)
                // adiciona na lista de candidatos apenas os pixels que não pertencem à interseção
                //  = adicionar o resultado da subtração: objeto preenchido - seus furos
                candidates_coords.Add(single_object.FindAll(x => !holesInObject.Contains(x))); 
        }

        // Efetua a verificação citada anteriormente.
        foreach (var valid_object in valid_object_coords)
        {
            foreach (var candidate in candidates_coords)
            { 
                if (valid_object.Intersect(candidate).Count() > 0 // Se existe interseção
                    && valid_object.Count() == candidate.Count() // Se o obj candidato possui a mesma quantidade de pixels que o obj válido
                    && valid_object.Count() > 3)                // Se o objeto válido pode conter furo
                {
                     objects_with_holes_count++; // Se sim, contabiliza o objeto que possui furo
                }
            }
        }
        Console.WriteLine("OBJETOS NA IMAGEM: " + valid_objects_count);
        Console.WriteLine("OBJETOS COM FUROS: " + objects_with_holes_count);
    }

    public void NegativeImg(PBMImage image) // Operação pontual: Negativo
    {
        for (int i = 0; i < image.GetImageSize().Width; i++)
        {
            for (int j = 0; j < image.GetImageSize().Height; j++)
            {
                if (image.GetPixel(i, j) == 1) image.SetPixel(i, j, 0);
                else image.SetPixel(i, j, 1);
            }
        }
    }

    public void FloodFillScanLine(PBMImage image)
    {
        Stack<Point> pixels = new Stack<Point>();
        NegativeImg(image);
        pixels.Push(new Point(0, 0));
        while (pixels.Count != 0)
        {
            Point temp = pixels.Pop();
            int y1 = temp.Y;
            while (y1 >= 0 && image.GetPixel(temp.X, y1) == 1)
            {
                y1--;
            }
            y1++;
            bool spanLeft = false;
            bool spanRight = false;
            while (y1 < image.GetImageSize().Height && image.GetPixel(temp.X, y1) == 1)
            {
                image.SetPixel(temp.X, y1, 0);

                if (!spanLeft && temp.X > 0 && image.GetPixel(temp.X - 1, y1) == 1)
                {
                    pixels.Push(new Point(temp.X - 1, y1));
                    spanLeft = true;
                }
                else if (spanLeft && temp.X - 1 == 0 && image.GetPixel(temp.X - 1, y1) != 1)
                {
                    spanLeft = false;
                }
                if (!spanRight && temp.X < image.GetImageSize().Width - 1 && image.GetPixel(temp.X + 1, y1) == 1)
                {
                    pixels.Push(new Point(temp.X + 1, y1));
                    spanRight = true;
                }
                else if (spanRight && temp.X < image.GetImageSize().Width - 1 && image.GetPixel(temp.X + 1, y1) != 1)
                {
                    spanRight = false;
                }
                y1++;
            }
        }
        image.PrintImgMatrix();
    }

    private void FloodFillDfs(PBMImage image) // Algoritmo de flood filling baseado em uma DFS
    {
        Stack<Point> pixels = new Stack<Point>();
        NegativeImg(image);
        pixels.Push(new Point(0, 0));
        while (pixels.Count > 0)
        {
            Point a = pixels.Pop();
            if (a.X < image.GetImageSize().Width && a.X >= 0 &&
                    a.Y < image.GetImageSize().Height && a.Y >= 0)
            {

                if (image.GetPixel(a.X, a.Y) == 1 )
                {
                    image.SetPixel(a.X, a.Y, 0);
                    pixels.Push(new Point(a.X - 1, a.Y));
                    pixels.Push(new Point(a.X + 1, a.Y));
                    pixels.Push(new Point(a.X, a.Y - 1));
                    pixels.Push(new Point(a.X, a.Y + 1));
                }
            }
        }
        image.PrintImgMatrix();
    }

    public PBMImage CloneImage(PBMImage image) // Método para efetuar uma deep copy na imagem
    {
        return new PBMImage(image.GetImageSize(), image.GetImgMatrix().ConvertAll(x => new List<byte>(x)));
    }

    public void CombineImages(PBMImage image1, PBMImage image2) // Faz a união de duas imagens
    {
        if(image1.GetImageSize() == image2.GetImageSize())
        {
            for(int i = 0; i < image1.GetImageSize().Width; i++)
            {
                for (int j = 0; j < image1.GetImageSize().Height; j++)
                {
                    if (image2.GetPixel(i, j) == 1) image1.SetPixel(i, j, 1);
                }
            }
        }
        else
        {
            throw new Exception("As imagens possuem tamanho diferente.");
        }
    }

    public void FillHoles(PBMImage image) // Preenche os furos de uma imagem
    {
        PBMImage img2 = CloneImage(image);
        FloodFillDfs(img2);
        CombineImages(image, img2);
    }
    #endregion

}


class PBMImage
{
    #region Attributes
    private (int x, int y) _img_size;
    private List<List<byte>> _img_matrix;
    #endregion

    #region Methods
    public PBMImage((int,int) size, List<List<byte>> matrix)
    {
        _img_size = size;
        _img_matrix = matrix;
    }

    public void PrintImgMatrix()
    {
        for (int i = 0; i < GetImageSize().Width; i++)
        {
            for (int j = 0; j < GetImageSize().Height; j++) Console.Write(GetPixel(i,j) + " ");
            Console.WriteLine("");
        }
        Console.WriteLine("");
    }

    public (int Width, int Height) GetImageSize()
    {
        return _img_size;
    }

    public List<List<byte>> GetImgMatrix()
    {
        return _img_matrix;
    }

    public void SetPixel(int x, int y, byte color)
    {
        _img_matrix[x][y] = color;
    }

    public byte GetPixel(int x, int y)
    {
        return _img_matrix[x][y];
    }
    #endregion
}

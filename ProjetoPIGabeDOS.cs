using System.Drawing;

internal class ProjetoPIGabeDOS
{
    //Discente: Gabriel de Oliveira Santos
    //Disciplina: Processamento de Imagens
    //Turma: 01

    static (bool debugMode, string? path) handleParams(string[] commandLineParams)
    {
        (bool debugMode, string? path) retorno = (false, null);

        List<string> executionParams = new List<string>();
        foreach (var arg in commandLineParams) executionParams.Add(arg);

        if (executionParams.IndexOf("-debugMode") != -1) retorno.debugMode = true;
        if (executionParams.IndexOf("-path") != -1) retorno.path = executionParams[executionParams.IndexOf("-path") + 1];
        return retorno;
    }

    static void Main(string[] args)
    {
        (bool debugMode, string? path) ret = handleParams(args);
        ImageLib ir = new ImageLib(debugMode: ret.debugMode);
        PBMImage? image = ir.ReadImg(ret.path);
        if (image != null) ir.GenericStuffSolution(image);
    }
}

class ImageLib
{
    #region Attributes
    private bool _debugMode = false;
    #endregion

    #region Methods
    public ImageLib(bool debugMode = false)
    {
        _debugMode = debugMode;
    }

    public PBMImage? ReadImg(String? path = null) // Efetua a leitura de uma imagem
    {
        if (path == null)
        {
            Console.WriteLine("Informe o caminho da imagem (com a extensão .pbm no final):");
            path = Console.ReadLine();
        }

        if (!path.Contains(".pbm")) path += (".pbm");

        if (File.Exists(path))
        {
            int headerLinesCount = 0;
            List<string> lines = new List<string>();
            bool isSinglePixelRow = false;
            (int X, int Y) imageSizeXY = (0, 0);
            String? currentLine;
            StreamReader sr = new StreamReader(path);

            while ((currentLine = sr.ReadLine()) != null) // Adiciona as linhas da imagem em uma lista
            {
                if (headerLinesCount < 2) // Declaração da imagem e comentário são dispensáveis
                {
                    headerLinesCount++;
                    continue;
                }
                if (headerLinesCount == 2) // Faz a leitura e armazena o tamanho da imagem
                {
                    String[] size = currentLine.Split(' ');
                    imageSizeXY = (int.Parse(size[0]), int.Parse(size[1]));
                    headerLinesCount++;
                }
                else // Adiciona os pixels na lista
                {
                    // Flag que identifica se o arquivo da imagem está distribuindo um pixel por linha
                    if (currentLine.Trim().Length == 1) isSinglePixelRow = true;
                    lines.Add(currentLine);
                }
            }

            if (!isSinglePixelRow) // Se não for um pixel por linha
            {
                if (lines[0].Split(' ').Length > 1) // Caso os pixels estejam separados por um ' '
                {
                    List<string> new_bytes = new List<string>();
                    foreach (var row in lines)
                    {
                        foreach (var pixel in row.Split(' ')) new_bytes.Add(pixel.ToString());
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
            int pixelsCount = 0;
            for (int i = 0; i < imageSizeXY.X; i++)
            {
                List<byte> row = new List<byte>();
                for (int j = 0; j < imageSizeXY.Y; j++)
                {
                    row.Add(byte.Parse(lines[pixelsCount]));
                    pixelsCount++;
                }
                matrix.Add(row);
            }

            // Instancia a imagem
            PBMImage img = new PBMImage(imageSizeXY, matrix);
            return img;
        }
        else
        {
            Console.WriteLine("Caminho inválido, execute o programa novamente.");
        }
        return null;
    }

    private void ExportImg(PBMImage image, string? path_with_name = null) // Salva a imagem em um arquivo. É usado quando o debugMode está ativado.
    {
        List<string> pbm_image = new List<string>();
        pbm_image.Add("P1");
        pbm_image.Add("#Criado por: Gabriel Oliveira.");
        pbm_image.Add($"{image.GetImageSize().Width} {image.GetImageSize().Height}");
        foreach (var row in image.GetImgMatrix())
        {
            pbm_image.Add(string.Join(' ', row.ToArray()));
        }
        if (path_with_name == null)
        {
            Console.WriteLine("Informe o diretório com o nome do arquivo de saída:");
            Console.WriteLine("Exemplo: C:/Users/gabriel/Desktop/imagem1.pbm");
            String? path = Console.ReadLine();
        }

        if (path_with_name != null && path_with_name.Contains(".pbm"))
        {
            using (StreamWriter sw = new StreamWriter(path_with_name))
            {
                foreach (var row in pbm_image) sw.WriteLine(row);
            }
        }
    }

    // Contabiliza e armazena os objetos de uma imagem
    public int CountObjectsInImg(PBMImage image, bool isObject, List<List<Point>> objects_coords)
    {
        // A logica desse método consiste em aplicar uma DFS (Depth-First Search) nos pixels e buscar por objetos compostos por 1's,
        //  utilizando uma matriz do mesmo tamanho da imagem original para sinalizar quais pixels já foram visitados.

        // A DFS avança de acordo com o tipo de pixel em destaque, caso seja um pixel de um objeto, a movimentação é em cruz 
        //	(4-neighborhood) e, caso seja um pixel de um furo, a movimentação é em estrela (8-neighborhood).

        // Além disso, este método junto a DFS além de contabilizar os objetos, armazenam os objetos (lista de pontos) encontrados
        //  em uma lista de objetos, ou seja, uma lista de lista de pontos;

        int objects_count = 0;
        List<List<byte>> visitedMatrix = new List<List<byte>>();
        List<Point> single_object = new List<Point>();

        for (int i = 0; i < image.GetImageSize().Height; i++)
        {
            visitedMatrix.Add(Enumerable.Repeat((byte)0, image.GetImageSize().Width).ToList());
        }

        for (int i = 0; i < image.GetImageSize().Width; i++)
        {
            for (int j = 0; j < image.GetImageSize().Height; j++)
            {
                if (DfsCountObjects(i, j, image, isObject, visitedMatrix, single_object))
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
    private bool DfsCountObjects(int x, int y, PBMImage image, bool isObject, List<List<byte>> visitedMatrix, List<Point> objeto)
    {
        List<Point> objectNeighbors = new List<Point>()
        {
            new Point(1,0), new Point(-1,0), new Point(0,1), new Point(0, -1), new Point(-1,1), new Point(1,1), new Point(1,-1), new Point(-1,-1)
        };

        List<Point> holeNeighbors = new List<Point>()
        {
            new Point(1,0), new Point(-1,0), new Point(0,1), new Point(0, -1)
        };

        if (x >= image.GetImageSize().Width || y >= image.GetImageSize().Height || x < 0 || y < 0) // Coordenadas fora da imagem
        {
            return false;
        }
        if (visitedMatrix[x][y] == 1) // Pixel já visitado
        {
            return false;
        }
        if (image.GetPixel(x, y) == 0) // Fundo da imagem
        {
            return false;
        }
        visitedMatrix[x][y] = 1;

        objeto.Add(new Point(x-1, y-1)); // Coordenadas corrigidas, pois são referentes a imagem sem bordas
        if (isObject)
        {
            foreach (var direction in objectNeighbors) // Executa a DFS para cada vizinho possível de um objeto
            {
                DfsCountObjects(x + direction.X, y + direction.Y, image, isObject, visitedMatrix, objeto);
            };
        }
        else
        {
            foreach (var direction in holeNeighbors) // Executa a DFS para cada vizinho possível de um furo
            {
                DfsCountObjects(x + direction.X, y + direction.Y, image, isObject, visitedMatrix, objeto);
            };
        }
        return true;
    }

    // Efetua o processo reverso da função de adição de borda
    private PBMImage RemoveBorderImg(PBMImage image)
    {
        List<List<byte>> outputImageMatrix = new List<List<byte>>();
        for (int i = 0; i < image.GetImageSize().Height - 2; i++)
        {
            outputImageMatrix.Add(Enumerable.Repeat((byte)0, image.GetImageSize().Width - 2).ToList());
        }

        for (int i = 1; i < image.GetImageSize().Width -1; i++)
        {
            for (int j = 1; j < image.GetImageSize().Height-1; j++)
            {
                if (image.GetPixel(i, j) == 1) outputImageMatrix[i - 1][j - 1] = image.GetPixel(i, j);
            }
        }
        return new PBMImage((image.GetImageSize().Width - 2, image.GetImageSize().Height - 2), outputImageMatrix);
    }

    // Adciona uma borda de 1 pixel em toda a imagem, atualizando sua matriz e o seu tamanho
    private PBMImage AddBorderImg(PBMImage image)
    {
        List<List<byte>> outputImageMatrix = new List<List<byte>>();
        for (int i = 0; i < image.GetImageSize().Height + 2; i++)
        {
            outputImageMatrix.Add(Enumerable.Repeat((byte)0, image.GetImageSize().Width + 2).ToList());
        }

        for (int i = 0; i < image.GetImageSize().Width; i++)
        {
            for (int j = 0; j < image.GetImageSize().Height; j++)
            {
                if (image.GetPixel(i, j) == 1) outputImageMatrix[i + 1][j + 1] = image.GetPixel(i, j);
            }
        }
        return new PBMImage((image.GetImageSize().Width + 2, image.GetImageSize().Height + 2), outputImageMatrix);
    }

    // A partir da lista de objetos de uma imagem, retorna uma lista de segmentos (lista de imagens contendo apenas um objeto)
    private List<PBMImage> GetImageSegments(PBMImage image, List<List<Point>> imageObjects)
    {
        List<PBMImage> Segments = new List<PBMImage>(); // Lista de segmentos
        int segmentsCounter = 0;

        if (_debugMode == true)
        {
            Console.WriteLine("Segmentos da imagem de acordo com os objetos encontrados: ");
            Console.WriteLine("Obs.: os segmentos foram exportados como imagens .PBM.");
        }

        foreach (var Object in imageObjects)
        {
            // Cria uma imagem temporária vazia
            PBMImage temp = CloneImage(image);
            temp.SetZeros();

            // Efetua a união da imagem vazia com o segmento
            foreach (var Pixel in Object) temp.SetPixel(Pixel.X, Pixel.Y, 1);
            
            Segments.Add(temp); // Adiciona na lista de retorno da função

            if (_debugMode)
            {
                ExportImg(temp, string.Format("./debug_segmento_{0}.pbm", segmentsCounter));
                temp.PrintImgMatrix();
                segmentsCounter++;
            }
        }
        return Segments;
    }

    private bool IsEmptyImage(PBMImage Image) // Verifica se a imagem dada é completamente composta por pixels 0
    {
        foreach(var Row in Image.GetImgMatrix())
        {
            if (Row.Contains(1)) return false;
        }
        return true;
    }

    // Dado uma lista de segmentos, retorna os que possuem furo
    private List<PBMImage> GetSegmentsWithHoles(List<PBMImage> imageSegments)
    {
        List<PBMImage> Segments = new List<PBMImage>(); // Retorno da função
        int segmentsCounter = 0;

        if (_debugMode == true)
        {
            Console.WriteLine("Segmentos (objetos) que possuem furos: ");
        }

        foreach (var image in imageSegments)
        {
            PBMImage temp = CloneImage(image);
            FloodFillDfs(temp, new Point(0, 0)); // Preenche todo o fundo da imagem
            NegativeImg(temp); // Aplica o negativo para obter apenas os furos
            if (!IsEmptyImage(temp)) // Caso a imagem não seja vazia (completamente preechida pela cor branca)
            {
                Segments.Add(RemoveBorderImg(image)); // Adiciona a imagem na lista de retorno
                if (_debugMode)
                {
                    ExportImg(temp, string.Format("./debug_segmento_furado_{0}.pbm", segmentsCounter));
                    image.PrintImgMatrix();
                    segmentsCounter++;
                }
            }
        }
        return Segments;
    }

    public void GenericStuffSolution(PBMImage image) // Método que soluciona o problema proposto no projeto
    {
        List<List<Point>> validObjectCoords = new List<List<Point>>(); // Armazena os objetos válidos

        if (_debugMode == true)
        {
            Console.WriteLine("Imagem de Origem:");
            image.PrintImgMatrix();
        }

        PBMImage img = AddBorderImg(CloneImage(image)); // Clona a imagem original e adiciona bordas à ela
        
        CountObjectsInImg(img, true, validObjectCoords); // Contabiliza os objetos e os armazena em uma lista

        // A partir da lista de objetos obtida, cria uma lista de imagens com cada uma contendo um objeto
        // = lista de planos da imagem = lista de segmentos.
        List<PBMImage> imageSegments = GetImageSegments(img, validObjectCoords);

        // A partir da lista de segmentos, verifica quais possuem furo e os retorna em uma nova lista
        List<PBMImage> segmentsWithHoles = GetSegmentsWithHoles(imageSegments);
        
        // A quantidade de segmentos em cada uma das listas representa a quantidade de objetos
        //  logo, basta printar os lengths das listas
        Console.WriteLine("Total de objetos na imagem: " + imageSegments.Count());
        Console.WriteLine("Total de objetos com pelo menos um furo: " + segmentsWithHoles.Count());
        
        img = RemoveBorderImg(img);
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

    private void FloodFillDfs(PBMImage image, Point starterPoint) // Algoritmo de flood filling baseado em uma DFS
    {
        Stack<Point> pixels = new Stack<Point>();
        pixels.Push(starterPoint);

        while (pixels.Count > 0)
        {
            Point temp = pixels.Pop();
            if (temp.X < image.GetImageSize().Width && temp.X >= 0 &&
                    temp.Y < image.GetImageSize().Height && temp.Y >= 0)
            {

                if (image.GetPixel(temp.X, temp.Y) == 0)
                {
                    image.SetPixel(temp.X, temp.Y, 1);
                    pixels.Push(new Point(temp.X - 1, temp.Y));
                    pixels.Push(new Point(temp.X + 1, temp.Y));
                    pixels.Push(new Point(temp.X, temp.Y - 1));
                    pixels.Push(new Point(temp.X, temp.Y + 1));
                }
            }
        }
    }

    public PBMImage CloneImage(PBMImage image) // Método para efetuar uma deep copy na imagem
    {
        return new PBMImage(image.GetImageSize(), image.GetImgMatrix().ConvertAll(x => new List<byte>(x)));
    }
    #endregion
}


class PBMImage
{
    #region Attributes
    private (int x, int y) imageSize;
    private List<List<byte>> imgMatrix;
    #endregion

    #region Methods
    public PBMImage((int, int) size, List<List<byte>> matrix)
    {
        imageSize = size;
        imgMatrix = matrix;
    }

    public void PrintImgMatrix()
    {
        for (int i = 0; i < GetImageSize().Width; i++)
        {
            for (int j = 0; j < GetImageSize().Height; j++) Console.Write(GetPixel(i, j) + " ");
            Console.WriteLine("");
        }
        Console.WriteLine();
    }

    public (int Width, int Height) GetImageSize()
    {
        return imageSize;
    }

    public List<List<byte>> GetImgMatrix()
    {
        return imgMatrix;
    }

    public void SetPixel(int x, int y, byte color)
    {
        imgMatrix[x][y] = color;
    }

    public byte GetPixel(int x, int y)
    {
        return imgMatrix[x][y];
    }

    public void SetZeros()
    {
        for (int i = 0; i < this.GetImageSize().Width; i++)
            for (int j = 0; j < this.GetImageSize().Height; j++)
                this.SetPixel(i, j, 0);
    }
    #endregion
}

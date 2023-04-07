using System.Drawing;

internal class Program
{
    private static void Main(string[] args)
    {
        Main();

        void Main()
        {
            ImageLib ir = new ImageLib();
            PBMImage image = ir.ReadImg("E:\\Github\\ProjetoPI\\furo3");
            //image.PrintImgMatrix();
            //ir.negativeImg(image);
            //ir.FillHoles(image);
            ir.CountValidHoles(image);
            //Console.WriteLine(ir.CountObjectsInImg(image));
        }
    }
}

class ImageLib
{
    public PBMImage? ReadImg(String? path = null)
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
            (int, int) image_size_xy = (0, 0);
            String? current_line;
            StreamReader sr = new StreamReader(path);

            while ((current_line = sr.ReadLine()) != null)
            {
                if (header_lines_count < 2)
                {
                    header_lines_count++;
                    continue;
                }
                if (header_lines_count == 2)
                {
                    String[] size = current_line.Split(' ');
                    image_size_xy = (int.Parse(size[0]), int.Parse(size[1]));
                    header_lines_count++;
                }
                else
                {
                    if(current_line.Trim().Length == 1) is_single_pixel_row = true;
                    lines.Add(current_line);
                }
            }

            if (!is_single_pixel_row)
            {
                if (lines[0].Split(' ').Length > 1)
                {
                    List<string> new_bytes = new List<string>();
                    foreach (var row in lines)
                    {
                        foreach(var pixel in row.Split(' ')) new_bytes.Add(pixel.ToString());
                    }
                    lines = new_bytes;
                }
                else
                {
                    List<char> bytes = new List<char>(string.Join(' ', lines.ToArray()).Select(x => x).ToList());
                    bytes.RemoveAll(x => x == ' ');
                    List<string> new_bytes = new List<string>();
                    foreach (var pixel in bytes) new_bytes.Add(pixel.ToString());
                    lines = new_bytes;
                }
            }

            PBMImage img = new PBMImage(image_size_xy);
            img.setImgMatrix(lines);
            //negativeImg(img);
            //img.PrintImgMatrix();
            //Console.WriteLine("result do count: " + CountObjectsInImg(img));
            //ExportImg(img);
            return img;
        }
        else
        {
            Console.WriteLine("Caminho inválido, execute o programa novamente.");
        }
        return null;
    }

    public void ExportImg(PBMImage image)
    {
        List<string> pbm_image = new List<string>();
        pbm_image.Add("P1");
        pbm_image.Add("#Criado por: Gabriel Oliveira.");
        pbm_image.Add($"{image.GetImageSize().Width} {image.GetImageSize().Height}");
        foreach (var row in image.GetImgMatrix())
        {
            pbm_image.Add(string.Join(' ', row.ToArray()));
        }
        Console.WriteLine("Informe o diretório com o nome do arquivo de saída:");
        Console.WriteLine("Exemplo: C:\\Users\\gabriel\\Desktop\\imagem1.pbm");
        //E:\Github\ProjetoPI\teste.pbm
        String? path = Console.ReadLine();

        if (path != null && path.Contains(".pbm"))
        {
            using (StreamWriter sw = new StreamWriter(path))
            {
                foreach (var row in pbm_image) sw.WriteLine(row);
            }
        }
    }

    public int CountObjectsInImg(PBMImage image, List<List<(int, int)>> objects_coords)
    {
        int objects_count = 0;
        List<List<byte>> visited_matrix = new List<List<byte>>();
        List<(int,int)> single_object = new List<(int,int)> ();

        for(int i = 0; i< image.GetImageSize().Width; i++)
        {
            visited_matrix.Add(Enumerable.Repeat((byte)0, image.GetImageSize().Width).ToList()); 
        }

        for (int i = 0; i < image.GetImageSize().Width; i++)
        {
            for(int j = 0; j < image.GetImageSize().Height; j++)
            {
                if (dfs_count_objects(i, j, image, visited_matrix, single_object))
                {
                    objects_coords.Add(single_object);
                    objects_count++;
                    single_object = new List<(int, int)>();
                }
                else
                {
                    single_object = new List<(int, int)>();
                }
            }
        }
        return objects_count;
    }

    private bool dfs_count_objects(int x, int y, PBMImage image, List<List<byte>> visited_matrix, List<(int,int)> objeto)
    {
        List<(int i, int j)> pixel_connections = new List<(int i, int j)>()
        {
            (1,0), (-1,0), (0,1), (0,-1)
        };
        if ( x >= image.GetImageSize().Width || y >= image.GetImageSize().Height || x  < 0 || y < 0)
        {
            return false;
        }
        if (visited_matrix[x][y] == 1)
        {
            return false;
        }
        if (image.GetPixel(x, y) == 0)
        {
            return false;
        }
        visited_matrix[x][y] = 1;
        objeto.Add((x, y));
        foreach (var direction in pixel_connections)
        {
            dfs_count_objects(x + direction.i, y + direction.j, image, visited_matrix, objeto);
        }

        return true;
    }

    public void CountValidHoles(PBMImage image)
    {
        List<List<(int, int)>> objects_coords = new List<List<(int, int)>>();
        List<List<(int, int)>> valid_coords = new List<List<(int, int)>>();

        List<List<(int, int)>> holes_coords = new List<List<(int, int)>>();
        List<List<(int, int)>> aaa = new List<List<(int, int)>>();
        
        PBMImage img2 = CloneImage(image);
        FloodFill(img2);
        int holesc = CountObjectsInImg(img2, holes_coords);

        int objetos_validos = CountObjectsInImg(image, valid_coords);
        FillHoles(image);
        int objetos = CountObjectsInImg(image, objects_coords);


        
        int objetosComFuro = 0;
        foreach (var single_object in objects_coords)
        {
            List <(int, int)> holesOfObject = new List<(int, int)> ();
            foreach (var hole in holes_coords)
            {
                if (single_object.Intersect(hole).Count() > 0) {
                    holesOfObject.AddRange(hole);
                }
            }
            if(single_object.Count() > 3 && holesOfObject.Count() > 0) aaa.Add(single_object.FindAll(x => !holesOfObject.Contains(x)));
        }

        foreach (var valid_object in valid_coords)
        {
            foreach (var candidate in aaa)
            {
                if (valid_object.Intersect(candidate).Count() > 0)
                {
                    if (valid_object.Count() == candidate.Count())
                    {
                        if(valid_object.Count() > 3) objetosComFuro++;
                    }
                }
            }
        }
        Console.WriteLine("Objs com furo: " + objetosComFuro);

    }

    public void countObjects(PBMImage image)
    {
        PBMImage img2 = CloneImage(image);

        List<List<(int, int)>> objects_coords = new List<List<(int, int)>>();
        List<List<(int, int)>> objects_coords2 = new List<List<(int, int)>>();

        List<List<(int, int)>> holes_coords = new List<List<(int, int)>>();

        int objetos = CountObjectsInImg(image, objects_coords);
        Console.WriteLine("OBJETOS NA IMAGEM: " + objetos);

        FillHoles(image);

        int furos = CountObjectsInImg(img2, holes_coords);
        Console.WriteLine("FUROS NA IMAGEM: " + furos);

        //TODO: algoritmo Flood Fill para remover o fundo do complemento da img original
        //      para depois retirar *apenas* os furos

        int objects_with_holes = verifyHolesImg(image, objects_coords, holes_coords, objects_coords2);
        Console.WriteLine("OBJETOS COM FUROS: " + objects_with_holes);
    }

    public int verifyHolesImg(PBMImage image, List<List<(int, int)>> objects, List<List<(int, int)>> holes
        , List<List<(int, int)>> valid_objects
        )
    {
        //Dictionary<int, bool> objects_with_holes= new Dictionary<int, bool>();
        int objects_with_holes = 0;
        foreach (var single_object in objects)
        {
            bool objectHasHole = false;
            foreach (var hole in holes)
            {
                //if (single_object.Intersect(hole).Count() > 0 ) objectHasHole = true;
                List<(int, int)> a = (List<(int, int)>)single_object.FindAll(x => !hole.Contains(x));
                foreach(var x in valid_objects) if(x.Intersect(a).Count() == x.Count()) objectHasHole = true;
            }
            if(objectHasHole) objects_with_holes++;
           // List<(int,int)> hole = new List<(int,int)> ();
            //_ = single_object.Select(x => hole.Contains(x));
            //valid_objects.Select(x => x.Contains());
        }
        return objects_with_holes;    
    }

    public void negativeImg(PBMImage image)
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
    

    public void FloodFill(PBMImage image)
    {
        Stack<Point> pixels = new Stack<Point>();
        negativeImg(image);
        pixels.Push(new Point(0,0));
        while (pixels.Count != 0)
        {
            Point temp = pixels.Pop();
            int y1 = temp.Y;
            while (y1 >= 0 && image.GetPixel(temp.X,y1) == 1)
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
                if (!spanRight && temp.X < image.GetImageSize().Width- 1 && image.GetPixel(temp.X + 1, y1) == 1)
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

    public PBMImage CloneImage(PBMImage image)
    {
        return new PBMImage(image.GetImageSize(), image.GetImgMatrix().ConvertAll(x => new List<byte>(x)));
    }

    public void CombineImages(PBMImage image1, PBMImage image2)
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
            throw new Exception("As imagens possuem tamanho diferente");
        }
    }

    public void FillHoles(PBMImage image)
    {
        PBMImage img2 = CloneImage(image);
        FloodFill(img2);
        CombineImages(image, img2);
        //image.PrintImgMatrix();
    }

    
}


class PBMImage
{
    #region Atributos
    private (int x, int y) _img_size;
    private List<List<byte>>? _img_matrix;
    #endregion

    public PBMImage((int,int) size, List<List<byte>>? matrix = null)
    {
        _img_size = size;
        _img_matrix = matrix == null? null : matrix;
    }

    public void setImgMatrix(List<string> pixels)
    {
        List<List<byte>> matrix = new List<List<byte>>();
        int pixels_count = 0;
        for (int i = 0; i < this.GetImageSize().Width; i++)
        {
            List<byte> row = new List<byte>();
            for (int j = 0; j < this.GetImageSize().Height; j++)
            {
                row.Add(byte.Parse(pixels[pixels_count]));
                pixels_count++;
            }
            matrix.Add(row);
        }
        _img_matrix = matrix;
    }

    public void SetPixel(int x, int y, byte color)
    {
        _img_matrix[x][y] = color;
    }

    public byte GetPixel(int x, int y)
    {
        return _img_matrix[x][y];
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

    private bool IsPixelInsideImage(Point pixel)
    {
        if (pixel.X >= GetImageSize().Width || pixel.Y >= GetImageSize().Height || pixel.X < 0 || pixel.Y < 0) return false;
        else return true;
    }

    public (int Width, int Height) GetImageSize()
    {
        return _img_size;
    }

    public List<List<byte>> GetImgMatrix()
    {
        return _img_matrix;
    }
}

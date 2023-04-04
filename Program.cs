using System.Reflection.Metadata.Ecma335;

Main();

void Main()
{
    ImageLib ir = new ImageLib();
    PBMImage image = ir.ReadImg("E:\\Github\\ProjetoPI\\aa");
    //Console.WriteLine(ir.CountObjectsInImg(image));
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
            //PrintImgMatrix(img);
            //ExportImg(img);
            return img;
        }
        return null;
    }

    public void PrintImgMatrix(PBMImage image)
    {
        for (int i = 0; i < image.img_size.x; i++)
        {
            for (int j = 0; j < image.img_size.y; j++) Console.Write(image.img_matrix[i][j] + " ");
            Console.WriteLine("");
        }
    }

    public void ExportImg(PBMImage image)
    {
        List<string> pbm_image = new List<string>();
        pbm_image.Add("P1");
        pbm_image.Add("#Criado por: Gabriel Oliveira.");
        pbm_image.Add($"{image.img_size.x} {image.img_size.y}");
        foreach (var row in image.img_matrix)
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

    public int CountObjectsInImg(PBMImage image)
    {
        int objects_count = 0;
        List<List<byte>> matrix_visitados = new List<List<byte>>();

        for(int i = 0; i< image.img_size.x; i++)
        {
            matrix_visitados.Add(Enumerable.Repeat((byte)0, image.img_size.x).ToList()); 
        }

        for (int i = 0; i < image.img_size.x; i++)
        {
            for(int j = 0; j < image.img_size.y; j++)
            {
                if (dfs_count_objects(i, j, image, matrix_visitados)) objects_count++;
            }
        }

        return objects_count;
    }

    private bool dfs_count_objects(int x, int y, PBMImage image, List<List<byte>> matrix_visistados)
    {
        List<(int i, int j)> pixel_connections = new List<(int i, int j)>()
        {
            (1,0), (-1,0), (0,1), (0,-1)
        };
        if ( x >= image.img_size.x || y >= image.img_size.y || x  < 0 || y < 0)
        {
            return false;
        }
        if (matrix_visistados[x][y] == 1)
        {
            return false;
        }
        if (image.img_matrix[x][y] == 0)
        {
            return false;
        }
        matrix_visistados[x][y] = 1;

        foreach (var direction in pixel_connections)
        {
            dfs_count_objects(x + direction.i, y + direction.j, image, matrix_visistados);
        }

        return true;
    }
    
}

class PBMImage
{
    #region Atributos
    private (int x, int y) _img_size;
    public (int x, int y) img_size => _img_size;
    private List<List<byte>>? _img_matrix;
    public List<List<byte>>? img_matrix => _img_matrix;
    #endregion

    public PBMImage((int,int) size)
    {
        _img_size = size;
        _img_matrix = null;
    }

    public void setImgMatrix(List<string> pixels)
    {
        List<List<byte>> matrix = new List<List<byte>>();
        int pixels_count = 0;
        for (int i = 0; i < this.img_size.x; i++)
        {
            List<byte> row = new List<byte>();
            for (int j = 0; j < this.img_size.y; j++)
            {
                row.Add(byte.Parse(pixels[pixels_count]));
                pixels_count++;
            }
            matrix.Add(row);
        }
        _img_matrix = matrix;
    }
}

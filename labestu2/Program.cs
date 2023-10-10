using Formatting = Newtonsoft.Json.Formatting;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Labestu2.Objetos;
using ARBOLAVl;
using System.IO;
using Newtonsoft.Json;


namespace Labestu2
{
    internal class Program
    {
        public static AVLTree<Ingreso> participante = new AVLTree<Ingreso>();
        static void Main(string[] args)
        {
            string ruta = "";
            Console.WriteLine("Ingrese la ruta del csv");
            ruta = Console.ReadLine();
            Console.WriteLine("Escriba la ruta de las cartas");
            string ruteletters = Console.ReadLine();
            Console.WriteLine("Escriba la ruta para guardar las cartas comprimidas");
            string rutelettercomp = Console.ReadLine();
            var reader = new StreamReader(File.OpenRead(ruta));
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                var value = line.Split(';');
                if (value[0] == "INSERT")
                {
                    var data = JsonConvert.DeserializeObject<participante>(value[1]);
                    participante trabajar = data;
                    List<string> dupli = trabajar.companies.Distinct().ToList();
                    trabajar.companies = dupli;
                    List<Compania> companias = new List<Compania>();
                    for (int i = 0; i < trabajar.companies.Count; i++)
                    {
                        Compania comp = new Compania();
                        comp.Name = trabajar.companies[i];
                        comp.Libreria.Build(comp.Name + "*" + trabajar.dpi);
                        byte[] estesi = comp.Libreria.Encode(comp.Name + "*" + trabajar.dpi);
                        //comp.texto = Encoding.ASCII.GetString(comp.cript);
                        string textoCodificado = string.Join("", estesi.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));

                        // Asigna la cadena al objeto Compania
                        comp.Codificado = textoCodificado;
                        string textoDecodificado = comp.Libreria.Decode(estesi);
                        comp.Decodificado = textoDecodificado;
                        companias.Add(comp);
                    }
                    Ingreso ingreso = new Ingreso();
                    ingreso.name = trabajar.name;
                    ingreso.dpi = trabajar.dpi;
                    ingreso.address = trabajar.address;
                    ingreso.dateBirth = trabajar.dateBirth;
                    ingreso.companies = companias;
                    string dpicomp = ingreso.dpi;
                    participante.insert(ingreso, ComparacioDPI);
                    string[] files = Directory.GetFiles(ruteletters);
                    Regex regex = new Regex(@"REC-" + dpicomp);
                    int numcart = 1;
                    foreach (string file in files)
                    {
                        Match match = regex.Match(file);
                        if (match.Success)
                        {
                            string text = System.IO.File.ReadAllText(file);
                            List<int> compress = LZW.Method.Compress(text);
                            string rutecompress = rutelettercomp + "\\" + "compressed-REC-" + dpicomp + "-" + Convert.ToString(numcart) + ".txt";
                            string ingresocomp = JsonConvert.SerializeObject(compress);
                            File.WriteAllText(rutecompress, ingresocomp);
                            numcart++;
                        }
                    }

                }
                else if (value[0] == "PATCH")
                {
                    var data = JsonConvert.DeserializeObject<participante>(value[1]);
                    participante trabajar = data;
                    Ingreso busqueda = new Ingreso();
                    busqueda.name = trabajar.name;
                    busqueda.dpi = trabajar.dpi;
                    if (participante.Search(busqueda, ComparacioDPI).name == trabajar.name)
                    {
                        if (trabajar.dateBirth != null)
                        {
                            participante.Search(busqueda, ComparacioDPI).dateBirth = trabajar.dateBirth;
                        }
                        if (trabajar.address != null)
                        {
                            participante.Search(busqueda, ComparacioDPI).address = trabajar.address;
                        }
                        if (trabajar.companies != null)
                        {
                            List<string> doble = trabajar.companies.Distinct().ToList();
                            List<Compania> nop = new List<Compania>();
                            for (int i = 0; i < doble.Count; i++)
                            {
                                Compania comp = new Compania();
                                comp.Name = doble[i];
                                comp.Libreria.Build(comp.Name + "*" + trabajar.dpi);
                                byte[] estesi = comp.Libreria.Encode(comp.Name + "*" + trabajar.dpi);
                                //comp.texto = Encoding.ASCII.GetString(comp.cript);
                                string textoCodificado = string.Join("", estesi.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));

                                // Asigna la cadena al objeto Compania
                                comp.Codificado = textoCodificado;
                                string textoDecodificado = comp.Libreria.Decode(estesi);
                                comp.Decodificado = textoDecodificado;

                                nop.Add(comp);
                            }
                            participante.Search(busqueda, ComparacioDPI).companies = nop;
                        }

                    }
                }
                else if (value[0] == "DELETE")
                {
                    var data = JsonConvert.DeserializeObject<participante>(value[1]);
                    participante trabajar = data;
                    Ingreso ingreso = new Ingreso();
                    ingreso.dpi = trabajar.dpi;
                    List<Ingreso> trabajo = participante.getAll();
                    int cant = trabajo.Count();
                    for (int i = 0; i < trabajo.Count; i++)
                    {
                        if (trabajo[i].dpi == ingreso.dpi)
                        {
                            trabajo.RemoveAt(i);
                        }
                    }
                    participante = new AVLTree<Ingreso>();
                    int cant2 = trabajo.Count();
                    for (int j = 0; j < trabajo.Count; j++)
                    {
                        participante.insert(trabajo[j], ComparacioDPI);
                    }
                }
            }
            bool basta = false;
            string parabasta = "si";
            while (basta == false)
            {
                string dpi;
                string rutesave;
                Console.WriteLine("Ingrese un numero de DPI");
                dpi = Console.ReadLine();
                Ingreso solicitudesearch = new Ingreso();
                Ingreso solicitudend = new Ingreso();
                solicitudesearch.dpi = dpi;
                solicitudend = participante.Search(solicitudesearch, ComparacioDPI);
                Console.WriteLine("Ruta para guardar el archivo (debe terminar en .json)");
                rutesave = Console.ReadLine();
                List<Ingreso> solicitantelist = new List<Ingreso>();
                solicitantelist.Add(solicitudend);
                Serializacion2(solicitantelist, rutesave);
                string[] compressfiles = Directory.GetFiles(rutelettercomp);
                Console.WriteLine("Escriba la ruta para guardar las cartas descomprimidas de " + dpi);
                string ruteletterdecode = Console.ReadLine();
                Regex regex2 = new Regex(@"compressed-REC-" + dpi);
                int letternum = 1;
                foreach (string cfile in compressfiles)
                {
                    Match m = regex2.Match(cfile);
                    if (m.Success)
                    {
                        var reader2 = new StreamReader(File.OpenRead(cfile));
                        List<int> compress = JsonConvert.DeserializeObject<List<int>>(reader2.ReadLine());
                        string decompress = LZW.Method.Decompress(compress);
                        string rutedecompress = ruteletterdecode + "\\" + "decompressed-REC-" + dpi + "-" + Convert.ToString(letternum) + ".txt";
                        File.WriteAllText(rutedecompress, decompress);
                        letternum++;
                    }
                }
                Console.WriteLine("¿Quiere hacer otra busqueda? si/no");
                parabasta = Console.ReadLine();
                if (parabasta == "no")
                {
                    basta = true;

                }
            }
        }
        public static bool ComparacioDPI(Ingreso persona, string operador, Ingreso persona2)
        {
            int Comparacion = string.Compare(persona.dpi, persona2.dpi);
            if (operador == "<")
            {
                return Comparacion < 0;
            }
            else if (operador == ">")
            {
                return Comparacion > 0;
            }
            else if (operador == "==")
            {
                return Comparacion == 0;
            }
            else return false;
        }
        public static void Serializacion2(List<Ingreso> Lista, string path)
        {
            string solictanteJson = JsonConvert.SerializeObject(Lista.ToArray(), Formatting.Indented);
            File.WriteAllText(path, solictanteJson);
        }
    }
}

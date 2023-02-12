using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using AngleSharp;
using AngleSharp.Dom;
using BookParser;
using Newtonsoft.Json;

namespace Parser;

public class ParserBook
{
    private const string Aut = "Автор";
    private const string Numb = "Количество страниц";

    public static IDocument GetDocument(string url)
    {
        var config = Configuration.Default.WithDefaultLoader();
        var context = BrowsingContext.New(config);
        return context.OpenAsync(url).Result;
    }
    private async Task<List<Book>> ParseBookInfo(string urlWithCollection,int startPage, int endPage)
    {
        Console.WriteLine($"Started thread since {startPage} to {endPage}");
        var books = new List<Book>();
        DateTime nowThread = DateTime.Now;
        for(int i = startPage; i <= endPage; i++)
        {
            try
            {
                DateTime now = DateTime.Now;
                var document2 = GetDocument(urlWithCollection + Convert.ToString(i));


                var textWitHResultSearchElements =
                    document2.GetElementsByClassName("flex flex-row xs:flex-col");
                
                if ((textWitHResultSearchElements.Length == 0))
                {
                    Console.WriteLine($"Page - {i}: Книги не найдены");
                    continue;
                }
                Console.WriteLine($"Page - {i} was readed, count = {textWitHResultSearchElements.Length}");
               // Console.WriteLine("Page - "+(i+1));
                foreach (var bookFromList in textWitHResultSearchElements)
                {
                    var book = ParseICollection(bookFromList);
                    books.Add(book);
                    Console.WriteLine(book.Name+' '+book.Author);
                    //Console.WriteLine($"Got book -  {book.Name}; Price - {book.Price}; Remainder - {book.Remainder}");
                    //Console.WriteLine();
                }
                DateTime end = DateTime.Now;
                Console.WriteLine($"Page - {i} was parsed, count in thread = {books.Count}; time - {new TimeSpan((end-now).Ticks).TotalMinutes} min");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} : ERROR for page- {ex.Message}");
            }
        }
        DateTime endThread = DateTime.Now;
        Console.WriteLine($"Thread parsed all pages.  time - {new TimeSpan((endThread-nowThread).Ticks).TotalMinutes} min");

        return books;
    }

    private Book ParseICollection(IElement element)
    {

        var refToBook = "https://agnibooks.ru" + element.GetElementsByClassName("w-full bg-neutral-100 rounded-md overflow-hidden cursor-pointer")[0]//возникли вопросы по тэгу а
            .Attributes["href"].Value;
        //Console.WriteLine("AddToRef is " + refToBook);
        var BookInfo = GetDocument(refToBook);
        string BoookName ="";
        int Remainder =0;
        int Price=0;
        string Description = "";
        String Author = "";
        string Genre = "";
        String Image = "";
        int NumberPages = 0;
        try
        {
            BoookName = BookInfo.GetElementsByTagName("h1")[0].TextContent;
           // Remainder = Int32.Parse(
            //    .TextContent.Split(' ')[0]);
            //var element2 = BookInfo.GetElementsByClassName("stock")[0].Children[0];
            //var element4 = element2.TextContent.Split(' ')[3];
            var div = element.GetElementsByClassName("xs:mt-2 text-sm text-neutral-500")[0];
            if(div != null)
            {
               // string res;
                bool result = int.TryParse(div.TextContent.Split(' ')[1], out Remainder);

            }
            // return null;



            //var PricE = BookInfo.GetElementsByClassName("flex flex-wrap text-2xl sm:text-3xl font-semibold text-header font-second items-center")[0].Children[0].TextContent;
            //Price = Int32.Parse(BookInfo.QuerySelector("div.price").TextContent.Replace(" ", ""));//GetElementsByClassName("price")[0].TextContent.Split(' ')[0]);
            Price = Int32.Parse(BookInfo.GetElementsByClassName("flex flex-wrap text-2xl sm:text-3xl font-semibold text-header font-second items-center")[0]
                .TextContent.Split(' ')[0]); //.Replace(" ", "");
            
            Description = BookInfo.GetElementsByClassName("text-sm sm:text-base space-y-5 page-content")[0].TextContent;
            var properties = BookInfo.GetElementsByClassName("py-1 grid grid-cols-2 xs:grid-cols-3 sm:grid-cols-4 px-0 xl:grid-cols-5");
            foreach (var Prop in properties)
            {
                string n = Prop.GetElementsByClassName("param-name relative")[0].TextContent;
                if (n.Contains(Aut))
                {
                    Author = Prop.GetElementsByClassName("text-sm mt-0 xs:col-span-2 xl:col-span-3")[0].TextContent; //В некоторых книгах автор отсутствует, заместо него становиться издатель и добавляется вконец блока поджанр другие авторы 
                }

                string v = Prop.GetElementsByClassName("param-name relative")[0].TextContent;
                if (v.Contains(Numb))
                {
                    NumberPages = int.Parse(Prop.GetElementsByClassName("text-sm mt-0 xs:col-span-2 xl:col-span-3")[0].TextContent);
                }
              
            }
   
            Genre = BookInfo.GetElementsByClassName("flex items-center flex-wrap")[0].GetElementsByTagName("li")[^1].TextContent
                .Replace(" ", "")
                .Replace("\t", "")
                .Replace("\n", "");
            //NumberPages = Int32.Parse(BookInfo.GetElementsByClassName("breadcrumb")[0].GetElementsByTagName("li")[3].TextContent.Replace(" ", ""));

            //var kk = element.GetElementsByClassName("max-h-full max-w-full mx-auto")[0];
            //Image = "agnibooks.ru" + BookInfo.GetElementsByClassName("swiper-slide swiper-slide-visible swiper-slide-active")[0].GetElementsByTagName("img")[0]
            //    .Attributes["src"]
            //    .Value
            //    .Replace(" ", "")
            //    .Replace("\t", "")
            //    .Replace("\n", "");
        }
        catch (Exception ex)
        {
       //     Console.WriteLine($"{DateTime.Now} : ERROR for book - {ex.Message}");
        }
        Book book = new Book()
        {
            Author = Author,
            Description = Description,
            Genre = Genre,
            Image = Image,
            Name = BoookName,
            Remainder = Remainder,
            Price = Price,
            NumberOfPages = 0
        };
        
        book.SourceName = "http://agnibooks.ru";
        return book;
    }
    
    public async Task StartParsingAsync()
    {
        Console.WriteLine("ВВЕДИТЕ СТРАНИЦУ С КОТОРОЙ ПАРСИТЬ, БЕЗ ПРОБЕЛОВ И ДРУГИХ ЗНАКОВ");
        //string startStr = Console.ReadLine();
        var finalBooks = new List<Book>();
        List<Book> parsedBooks1 = new List<Book>();
     
        var address = "https://agnibooks.ru/catalog/knigi?page=";
        var count = 305;
        var countOne = 19;
        int start = 1;
        Task t1 = Task.Run(async () =>
        {
            parsedBooks1 = await ParseBookInfo(address, start, countOne + start);
            
            WriteToJSON("BooksFromAgnibooks.json", parsedBooks1);
            parsedBooks1.Clear();
            
        });
        
        Task.WaitAll(t1);
        Console.ReadLine();
    }

    private void WriteToJSON(string path, List<Book> books)
    {
        var json = JsonConvert.SerializeObject(books);
        File.WriteAllText(path, json, Encoding.UTF8);
        Console.WriteLine("Books writed to json, count "+books.Count);
        File.WriteAllText(path+"Count",Convert.ToString(books.Count));
    }
    
    private string GetValue(IHtmlCollection<IElement> collection)
    {
        if (collection.Length > 0)
            return collection[0].TextContent;
        return "";
    }

    private IElement GetElement(IHtmlCollection<IElement> collection)
    {
        if (collection.Length > 0)
            return collection[0];
        return null;
    }
}
using LibraryManagement.Api.Common;
using LibraryManagement.Api.Models;

namespace LibraryManagement.Api.Data;

public static class SeedData
{
    public static List<User> GetSeedUsers()
    {
        return
        [
            new User
            {
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = UserRole.Admin,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                FirstName = "Librarian",
                LastName = "User",
                Email = "librarian@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Librarian@123"),
                Role = UserRole.Librarian,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                FirstName = "Member",
                LastName = "User",
                Email = "member@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Member@123"),
                Role = UserRole.Member,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
            
        ];
    }

    public static List<Author> GetSampleAuthors()
    {
        var authorNames = GetSampleBookData().Select(b => b.author).Distinct().ToList();
        return authorNames.Select(name => new Author { Name = name }).ToList();
    }

    public static List<Book> GetSampleBooks(Dictionary<string, int> authorLookup)
    {
        return GetSampleBookData().Select(b => new Book
        {
            Title = b.Title,
            Subtitle = b.Subtitle,
            AuthorId = authorLookup[b.author],
            ISBN = b.ISBN,
            ISBN13 = b.ISBN13,
            Category = b.Category,
            Language = b.Language,
            PublishedYear = b.PublishedYear,
            Edition = b.Edition,
            Description = b.Description,
            CoverImage = BookCoverGenerator.GenerateDataUri(b.Title, b.author, b.Category)
        }).ToList();
    }

    public static List<Copy> GetSampleCopies(Dictionary<string, int> bookLookup)
    {
        var copyCounts = GetCopyCountData();
        var copies = new List<Copy>();
        foreach (var (title, total, available) in copyCounts)
        {
            var bookId = bookLookup[title];
            for (int i = 1; i <= total; i++)
            {
                copies.Add(new Copy
                {
                    BookId = bookId,
                    Barcode = $"SEED-{bookId}-{i:D3}",
                    Status = i <= available ? "Available" : "Issued",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        return copies;
    }

   private static List<(string Title, string? Subtitle, string author, string ISBN, string? ISBN13, string Category, string Language, int PublishedYear, string? Edition, string Description, string? CoverImage)> GetSampleBookData()
{
    return
    [
        // 1. Classic Literature
        (Title: "Pride and Prejudice", Subtitle: null, author: "Jane Austen", ISBN: "978-0141439518", ISBN13: "9780141439518", Category: "Classic", Language: "English", PublishedYear: 1813, Edition: "Penguin Classics Edition", Description: "The story of Elizabeth Bennet and Mr. Darcy, exploring themes of love, class, and society in 19th century England. One of the most beloved novels in English literature.", CoverImage: "https://covers.openlibrary.org/b/isbn/9780141439518-L.jpg"),
        
        // 2. Modern Classic
        (Title: "The Catcher in the Rye", Subtitle: null, author: "J.D. Salinger", ISBN: "978-0316769488", ISBN13: "9780316769488", Category: "Classic", Language: "English", PublishedYear: 1951, Edition: "1st Edition", Description: "The story of Holden Caulfield, a teenage boy who has been expelled from prep school and wanders around New York City, struggling with alienation and identity.", CoverImage: "https://covers.openlibrary.org/b/isbn/9780316769488-L.jpg"),
        
        // 3. Contemporary Fiction
        (Title: "The Kite Runner", Subtitle: null, author: "Khaled Hosseini", ISBN: "978-1594631931", ISBN13: "9781594631931", Category: "Fiction", Language: "English", PublishedYear: 2003, Edition: "10th Anniversary Edition", Description: "Amir, a privileged Pashtun boy, and Hassan, his loyal Hazara friend, are inseparable. But when a brutal incident divides them, Amir's cowardice haunts him for decades.", CoverImage: "https://covers.openlibrary.org/b/isbn/9781594631931-L.jpg"),
        
        // 4. Thriller
        (Title: "Gone Girl", Subtitle: null, author: "Gillian Flynn", ISBN: "978-0307588371", ISBN13: "9780307588371", Category: "Thriller", Language: "English", PublishedYear: 2012, Edition: "1st Edition", Description: "On their fifth wedding anniversary, Nick Dunne's wife Amy disappears. As the police investigation unfolds, the media turns Nick into a suspect, and dark secrets about their marriage emerge.", CoverImage: "https://covers.openlibrary.org/b/isbn/9780307588371-L.jpg"),
        
        // 5. Sci-Fi Classic
        (Title: "Foundation", Subtitle: null, author: "Isaac Asimov", ISBN: "978-0553293357", ISBN13: "9780553293357", Category: "Science Fiction", Language: "English", PublishedYear: 1951, Edition: "1st Edition", Description: "Set in a galactic empire, mathematician Hari Seldon develops a science called psychohistory that can predict the future. He creates a foundation to preserve knowledge and shorten the dark ages that will follow the empire's fall.", CoverImage: "https://covers.openlibrary.org/b/isbn/9780553293357-L.jpg"),
        
        // 6. Fantasy
        (Title: "Harry Potter and the Sorcerer's Stone", Subtitle: null, author: "J.K. Rowling", ISBN: "978-0439708182", ISBN13: "9780439708182", Category: "Fantasy", Language: "English", PublishedYear: 1997, Edition: "1st American Edition", Description: "The first book in the legendary Harry Potter series. Harry discovers he is a wizard, attends Hogwarts School of Witchcraft and Wizardry, and uncovers the mystery of the Sorcerer's Stone.", CoverImage: "https://covers.openlibrary.org/b/isbn/9780439708182-L.jpg"),
        
        // 7. Mystery
        (Title: "The Girl on the Train", Subtitle: null, author: "Paula Hawkins", ISBN: "978-1594634024", ISBN13: "9781594634024", Category: "Mystery", Language: "English", PublishedYear: 2015, Edition: "1st Edition", Description: "Rachel takes the same commuter train every morning, watching a couple in a house along the route. When the woman goes missing, Rachel becomes entangled in the investigation.", CoverImage: "https://covers.openlibrary.org/b/isbn/9781594634024-L.jpg"),
        
        // 8. Historical Fiction
        (Title: "All the Light We Cannot See", Subtitle: null, author: "Anthony Doerr", ISBN: "978-1476746586", ISBN13: "9781476746586", Category: "Historical Fiction", Language: "English", PublishedYear: 2014, Edition: "1st Edition", Description: "A blind French girl and a German boy whose paths collide in occupied France during World War II. A breathtakingly beautiful story about humanity and survival.", CoverImage: "https://covers.openlibrary.org/b/isbn/9781476746586-L.jpg"),
        
        // 9. Philosophy
        (Title: "The Art of War", Subtitle: null, author: "Sun Tzu", ISBN: "978-1590302256", ISBN13: "9781590302256", Category: "Philosophy", Language: "English", PublishedYear: 500, Edition: "Shambhala Classics Edition", Description: "The oldest military treatise in the world, 'The Art of War' is a masterpiece of strategy and philosophy. Its teachings on conflict, leadership, and competition apply far beyond the battlefield.", CoverImage: "https://covers.openlibrary.org/b/isbn/9781590302256-L.jpg"),
        
        // 10. Biography
        (Title: "Steve Jobs", Subtitle: null, author: "Walter Isaacson", ISBN: "978-1451648539", ISBN13: "9781451648539", Category: "Biography", Language: "English", PublishedYear: 2011, Edition: "1st Edition", Description: "Based on more than forty interviews with Steve Jobs conducted over two years, this is the definitive portrait of the co-founder of Apple, a creative entrepreneur whose passion for perfection revolutionized six industries.", CoverImage: "https://covers.openlibrary.org/b/isbn/9781451648539-L.jpg"),
        
        // 11. Science
        (Title: "The Selfish Gene", Subtitle: "40th Anniversary Edition", author: "Richard Dawkins", ISBN: "978-0198788607", ISBN13: "9780198788607", Category: "Science", Language: "English", PublishedYear: 1976, Edition: "40th Anniversary Edition", Description: "A revolutionary work that presented evolution from the gene's point of view, introducing the concept of the 'meme' and changing how we think about natural selection.", CoverImage: "https://covers.openlibrary.org/b/isbn/9780198788607-L.jpg"),
        
        // 12. Psychology
        (Title: "Thinking, Fast and Slow", Subtitle: null, author: "Daniel Kahneman", ISBN: "978-0374533557", ISBN13: "9780374533557", Category: "Psychology", Language: "English", PublishedYear: 2011, Edition: "1st Edition", Description: "The Nobel Prize-winning psychologist explains the two systems that drive the way we think: System 1 is fast and intuitive, System 2 is slow and deliberate. A groundbreaking work in behavioral economics.", CoverImage: "https://covers.openlibrary.org/b/isbn/9780374533557-L.jpg"),
        
        // 13. Dystopian
        (Title: "Brave New World", Subtitle: null, author: "Aldous Huxley", ISBN: "978-0060850524", ISBN13: "9780060850524", Category: "Dystopian", Language: "English", PublishedYear: 1932, Edition: "1st Edition", Description: "A dystopian vision of a society where people are genetically modified, conditioned from birth, and kept in a state of blissful ignorance through the use of a drug called 'soma.'", CoverImage: "https://covers.openlibrary.org/b/isbn/9780060850524-L.jpg"),
        
        // 14. Business
        (Title: "The 7 Habits of Highly Effective People", Subtitle: "Powerful Lessons in Personal Change", author: "Stephen R. Covey", ISBN: "978-1982137274", ISBN13: "9781982137274", Category: "Business", Language: "English", PublishedYear: 1989, Edition: "30th Anniversary Edition", Description: "A holistic approach to solving personal and professional problems, the book teaches principles of fairness, integrity, honesty, and human dignity that lead to effectiveness and fulfillment.", CoverImage: "https://covers.openlibrary.org/b/isbn/9781982137274-L.jpg"),
        
        // 15. Historical
        (Title: "The Diary of a Young Girl", Subtitle: "The Definitive Edition", author: "Anne Frank", ISBN: "978-0553296983", ISBN13: "9780553296983", Category: "History", Language: "English", PublishedYear: 1947, Edition: "Definitive Edition", Description: "The poignant diary of Anne Frank, a Jewish teenager who hid with her family in an attic in Amsterdam during the Nazi occupation. A testament to human spirit and resilience.", CoverImage: "https://covers.openlibrary.org/b/isbn/9780553296983-L.jpg"),
        
        // 16. Thriller
        (Title: "The Whistler", Subtitle: null, author: "John Grisham", ISBN: "978-0345542465", ISBN13: "9780345542465", Category: "Thriller", Language: "English", PublishedYear: 2016, Edition: "1st Edition", Description: "A legal thriller about a corrupt judge, a tribal casino, and a whistleblower who knows too much. Investigator Lacy Stoltz uncovers a conspiracy that could bring down the Florida judicial system.", CoverImage: "https://covers.openlibrary.org/b/isbn/9780345542465-L.jpg"),
        
        // 17. Fantasy
        (Title: "The Name of the Wind", Subtitle: "The Kingkiller Chronicle: Day One", author: "Patrick Rothfuss", ISBN: "978-0756404741", ISBN13: "9780756404741", Category: "Fantasy", Language: "English", PublishedYear: 2007, Edition: "1st Edition", Description: "Kvothe, the legendary hero turned innkeeper, recounts his life story. From his childhood as a traveling performer to his education at the University, his tale is one of magic, music, and mystery.", CoverImage: "https://covers.openlibrary.org/b/isbn/9780756404741-L.jpg"),
        
        // 18. Science
        (Title: "Cosmos", Subtitle: null, author: "Carl Sagan", ISBN: "978-0345539434", ISBN13: "9780345539434", Category: "Science", Language: "English", PublishedYear: 1980, Edition: "1st Edition", Description: "The bestselling science book of all time. Carl Sagan takes us on a journey through the universe, exploring the history of astronomy, the search for extraterrestrial life, and our place in the cosmos.", CoverImage: "https://covers.openlibrary.org/b/isbn/9780345539434-L.jpg"),
        
        // 19. Self-Help
        (Title: "You Are a Badass", Subtitle: "How to Stop Doubting Your Greatness and Start Living an Awesome Life", author: "Jen Sincero", ISBN: "978-0762447695", ISBN13: "9780762447695", Category: "Self-Help", Language: "English", PublishedYear: 2013, Edition: "1st Edition", Description: "A fun, energetic guide to overcoming self-doubt and creating a life you love. Practical advice, spiritual insights, and inspiring stories to help you become your best self.", CoverImage: "https://covers.openlibrary.org/b/isbn/9780762447695-L.jpg"),
        
        // 20. Fiction
        (Title: "The Goldfinch", Subtitle: null, author: "Donna Tartt", ISBN: "978-0316055437", ISBN13: "9780316055437", Category: "Fiction", Language: "English", PublishedYear: 2013, Edition: "1st Edition", Description: "A young boy in New York is taken in by a wealthy family after his mother dies in a terrorist attack at a museum. He steals a priceless painting, setting off a chain of events spanning decades.", CoverImage: "https://covers.openlibrary.org/b/isbn/9780316055437-L.jpg"),
        
        // 21. Memoir
        (Title: "Eat, Pray, Love", Subtitle: "One Woman's Search for Everything Across Italy, India and Indonesia", author: "Elizabeth Gilbert", ISBN: "978-0143038412", ISBN13: "9780143038412", Category: "Memoir", Language: "English", PublishedYear: 2006, Edition: "1st Edition", Description: "After a painful divorce, Elizabeth Gilbert travels to Italy to enjoy pleasure, India to find spirituality, and Bali to seek balance. A journey of self-discovery that resonated with millions.", CoverImage: "https://covers.openlibrary.org/b/isbn/9780143038412-L.jpg"),
        
        // 22. Thriller
        (Title: "The Woman in the Window", Subtitle: null, author: "A.J. Finn", ISBN: "978-0062678416", ISBN13: "9780062678416", Category: "Thriller", Language: "English", PublishedYear: 2018, Edition: "1st Edition", Description: "Agoraphobic former child psychologist Anna Fox spends her days spying on her neighbors. When she witnesses a crime in the house across the street, she becomes entangled in a dangerous mystery.", CoverImage: "https://covers.openlibrary.org/b/isbn/9780062678416-L.jpg"),
        
        // 23. Historical Fiction
        (Title: "The Nightingale", Subtitle: null, author: "Kristin Hannah", ISBN: "978-0312577223", ISBN13: "9780312577223", Category: "Historical Fiction", Language: "English", PublishedYear: 2015, Edition: "1st Edition", Description: "Two sisters in France during World War II take different paths to survive. One joins the Resistance, the other tries to protect her family, both risking everything to help others.", CoverImage: "https://covers.openlibrary.org/b/isbn/9780312577223-L.jpg"),
        
        // 24. Technology
        (Title: "The Pragmatic Programmer", Subtitle: "Your Journey to Mastery", author: "David Thomas, Andrew Hunt", ISBN: "978-0135957059", ISBN13: "9780135957059", Category: "Technology", Language: "English", PublishedYear: 1999, Edition: "2nd Edition", Description: "A classic guide to software development that covers practical approaches, best practices, and philosophy. Essential reading for programmers at any level.", CoverImage: "https://covers.openlibrary.org/b/isbn/9780135957059-L.jpg"),
        
        // 25. Contemporary Fiction
        (Title: "The Fault in Our Stars", Subtitle: null, author: "John Green", ISBN: "978-0142424179", ISBN13: "9780142424179", Category: "Fiction", Language: "English", PublishedYear: 2012, Edition: "1st Edition", Description: "Hazel and Augustus are two teenagers who meet at a cancer support group. Their love story is funny, heartbreaking, and profound, exploring life, death, and the meaning of existence.", CoverImage: "https://covers.openlibrary.org/b/isbn/9780142424179-L.jpg")
    ];
}

private static List<(string title, int total, int available)> GetCopyCountData()
{
    return
    [
        ("Pride and Prejudice", 4, 2),
        ("The Catcher in the Rye", 5, 3),
        ("The Kite Runner", 6, 4),
        ("Gone Girl", 5, 3),
        ("Foundation", 4, 2),
        ("Harry Potter and the Sorcerer's Stone", 7, 5),
        ("The Girl on the Train", 5, 3),
        ("All the Light We Cannot See", 4, 2),
        ("The Art of War", 3, 1),
        ("Steve Jobs", 4, 2),
        ("The Selfish Gene", 3, 1),
        ("Thinking, Fast and Slow", 5, 3),
        ("Brave New World", 4, 2),
        ("The 7 Habits of Highly Effective People", 6, 4),
        ("The Diary of a Young Girl", 5, 3),
        ("The Whistler", 4, 2),
        ("The Name of the Wind", 5, 3),
        ("Cosmos", 4, 2),
        ("You Are a Badass", 3, 1),
        ("The Goldfinch", 4, 2),
        ("Eat, Pray, Love", 5, 3),
        ("The Woman in the Window", 4, 2),
        ("The Nightingale", 5, 3),
        ("The Pragmatic Programmer", 3, 1),
        ("The Fault in Our Stars", 6, 4)
    ];
}
}

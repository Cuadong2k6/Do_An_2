using Dapper;
using DAL.Helper;
using Model;
using System.Data;

namespace DAL
{
    public class BookRepository
    {
        private readonly IDatabaseHelper _db;

        public BookRepository(IDatabaseHelper db)
        {
            _db = db;
        }

        public async Task<(IEnumerable<BookModel> items, long total)> timkiemsachnangcao(
            string? keyword, string? theloai, string? tacgia, int? namxuatban,
            int pageIndex = 1, int pageSize = 10)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            var p = new DynamicParameters();
            p.Add("@keyword",    keyword,    DbType.String);
            p.Add("@theloai",    theloai,    DbType.String);
            p.Add("@tacgia",     tacgia,     DbType.String);
            p.Add("@namxuatban", namxuatban, DbType.Int32);
            p.Add("@page_index", pageIndex,  DbType.Int32);
            p.Add("@page_size",  pageSize,   DbType.Int32);
            p.Add("@total",      dbType: DbType.Int64, direction: ParameterDirection.Output);

            var items = await conn.QueryAsync<BookModel>("sp_book_search", p, commandType: CommandType.StoredProcedure);
            long total = p.Get<long>("@total");
            return (items, total);
        }

        public async Task<BookModel?> laychitietsach(Guid bookId)
        {
            return await _db.QueryFirstOrDefaultAsync<BookModel>("sp_book_getbyid", new { book_id = bookId });
        }

        public async Task themmoisach(BookModel model)
        {
            await _db.ExecuteAsync("sp_book_create", new
            {
                book_id      = model.book_id,
                title        = model.title,
                isbn         = model.isbn,
                tacgia       = model.tacgia,
                theloai      = model.theloai,
                nxb          = model.nxb,
                namxuatban   = model.namxuatban,
                mota         = model.mota,
                image_url    = model.image_url,
                tongsobancao = model.tongsobancao
            });
        }

        public async Task capnhatsach(BookModel model)
        {
            await _db.ExecuteAsync("sp_book_update", new
            {
                book_id      = model.book_id,
                title        = model.title,
                isbn         = model.isbn,
                tacgia       = model.tacgia,
                theloai      = model.theloai,
                nxb          = model.nxb,
                namxuatban   = model.namxuatban,
                mota         = model.mota,
                image_url    = model.image_url,
                tongsobancao = model.tongsobancao
            });
        }

        public async Task xoasach(Guid bookId)
        {
            await _db.ExecuteAsync("sp_book_delete", new { book_id = bookId });
        }
    }
}

using DAL;
using Model.Shared;

namespace BLL
{
    /// <summary>Xem nhật ký thay đổi sách / bạn đọc. Chỉ đọc — không có thao tác
    /// ghi, sửa hay khôi phục. Muốn sửa lại thì lấy giá trị ở cột "sau" nhập tay.</summary>
    public class NhatkyService
    {
        private readonly NhatkyRepository _nhatkyRepo;

        public NhatkyService(NhatkyRepository nhatkyRepo)
        {
            _nhatkyRepo = nhatkyRepo;
        }

        public async Task<ResponseModel> danhsachnhatky(string? keyword, string? bang, string? hanhdong, int page, int pageSize)
        {
            try
            {
                var (items, total) = await _nhatkyRepo.danhsachnhatky(keyword, bang, hanhdong, page, pageSize);
                return ResponseModel.Ok(items, "Thành công", total, page, pageSize);
            }
            catch (Exception ex)
            {
                return ResponseModel.Fail(ex.Message);
            }
        }
    }
}
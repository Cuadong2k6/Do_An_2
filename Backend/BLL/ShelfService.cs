using DAL;
using Microsoft.Extensions.Logging;
using Model;

namespace BLL
{
    public class ShelfService
    {
        private readonly ShelfRepository _shelfRepo;
        private readonly ILogger<ShelfService> _logger;

        public ShelfService(ShelfRepository shelfRepo, ILogger<ShelfService> logger)
        {
            _shelfRepo = shelfRepo;
            _logger    = logger;
        }

        public async Task<ResponseModel> danhsachke(string? keyword, int page, int pageSize)
        {
            var (items, total) = await _shelfRepo.danhsachke(keyword, page, pageSize);
            return ResponseModel.Ok(items, totalItems: total, page: page, pageSize: pageSize);
        }

        public async Task<ResponseModel> themke(ShelfModel model)
        {
            if (string.IsNullOrWhiteSpace(model.location_code))
                return ResponseModel.Fail("Mã kệ không được để trống.");
            try
            {
                await _shelfRepo.themke(model);
                _logger.LogInformation("Thêm kệ sách: {LocationCode}", model.location_code);
                return ResponseModel.Ok(null, "Thêm kệ sách thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi thêm kệ sách {LocationCode}", model.location_code);
                return ResponseModel.Fail(ex.Message);
            }
        }

        public async Task<ResponseModel> capnhatke(int shelfId, ShelfModel model)
        {
            if (shelfId <= 0) return ResponseModel.Fail("Mã kệ không hợp lệ.");
            if (string.IsNullOrWhiteSpace(model.location_code))
                return ResponseModel.Fail("Mã kệ không được để trống.");
            try
            {
                await _shelfRepo.capnhatke(shelfId, model);
                _logger.LogInformation("Cập nhật kệ sách {ShelfId}", shelfId);
                return ResponseModel.Ok(null, "Cập nhật kệ sách thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật kệ sách {ShelfId}", shelfId);
                return ResponseModel.Fail(ex.Message);
            }
        }
    }
}

// ==========================================================================
// Quản Lý Đặt Chỗ (Admin)
// API Endpoints:
//   GET  /api/Reservation?trangthai=&page=&pageSize=   → danh sách đặt chỗ
//   PUT  /api/Reservation/{resId}/receive             → xác nhận đã nhận
//   PUT  /api/Reservation/{resId}/cancel              -> huỷ đặt chỗ
//   PUT  /api/Reservation/expire                      -> cập nhật hết hạn
// ==========================================================================

document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('resTableBody')) {
        TaiDatCho(); // mặc định: trạng thái 0 (đang chờ)
    }
});

let _currentResTab = 0; // 0,1,2,3,null
let _trangRes = 1;
const HANG_RES = 10;

// Tìm kiếm
function TimkiemDatCho() {
    _trangRes = 1;
    TaiDatCho();
}

// Chuyển tab trạng thái
function ChuyenTabDatCho(trangthai) {
    _currentResTab = trangthai;
    _trangRes = 1;
    
    // Cập nhật UI
    ['Waiting', 'Received', 'Cancelled', 'Expired', 'All'].forEach(key => {
        const btn = document.getElementById('resTab' + key);
        if (btn) {
            let isActive = false;
            if (key === 'All' && trangthai === null) isActive = true;
            else if (key !== 'All' && parseInt(key[0]) === trangthai) isActive = true;
            
            btn.classList.toggle('btn-primary', isActive);
            btn.classList.toggle('btn-secondary', !isActive);
        }
    });
    
    TaiDatCho();
}

// Gọi khi bấm nút số trang
function DiTrangDatCho(tr) {
    _trangRes = tr;
    TaiDatCho();
}

// Load danh sách đặt chỗ
async function TaiDatCho() {
    const tbody = document.getElementById('resTableBody');
    tbody.innerHTML = '<tr><td colspan="7" style="text-align:center">Đang tải dữ liệu...</td></tr>';
    
    try {
        const keyword = (document.getElementById('resSearchInput')?.value || '').trim();
        
        const params = new URLSearchParams({ page: _trangRes, pageSize: HANG_RES });
        if (keyword) params.set('keyword', keyword);
        if (_currentResTab !== null) params.set('trangthai', _currentResTab);
        
        const res = await window.api.get(`/Reservation?${params.toString()}`);
        const tong = res.totalItems ?? 0;
        
        if (res.success && (!res.data || res.data.length === 0) && _trangRes > 1) {
            _trangRes = 1;
            return TaiDatCho();
        }
        
        if (res.success && res.data && res.data.length > 0) {
            tbody.innerHTML = '';
            res.data.forEach(rv => {
                const ngaydat = rv.res_date ? new Date(rv.res_date).toLocaleDateString('vi-VN') : '—';
                const han = rv.expiry_date ? new Date(rv.expiry_date).toLocaleDateString('vi-VN') : '—';
                
                const trangthaiMap = { 0: 'badge-warning', 1: 'badge-success', 2: 'badge-secondary', 3: 'badge-danger' };
                const trangthaiText = { 0: 'Đang chờ', 1: 'Đã nhận', 2: 'Đã huỷ', 3: 'Hết hạn' };
                const badgeClass = trangthaiMap[rv.trangthai] || 'badge-secondary';
                const badgeText  = trangthaiText[rv.trangthai] || 'Không rõ';
                
                // Nút thao tác
                let actions = '—';
                if (rv.trangthai === 0) {
                    // Đang chờ: có thể xác nhận nhận hoặc huỷ
                    const hetHan = rv.expiry_date && new Date(rv.expiry_date) < new Date();
                    actions = `
                        <button class="btn btn-primary" style="padding:5px 10px;font-size:0.8rem" onclick="XacNhanNhan('${rv.res_id}')">
                            <i class="fa-solid fa-check"></i> Xác Nhận Nhận
                        </button>
                        <button class="btn btn-danger" style="padding:5px 10px;font-size:0.8rem;margin-left:5px" onclick="HuyDatCho('${rv.res_id}')">
                            <i class="fa-solid fa-xmark"></i> Huỷ
                        </button>
                    `;
                } else if (rv.trangthai === 1) {
                    actions = '<span class="badge badge-success">Đã hoàn tất</span>';
                }
                
                tbody.innerHTML += `
                    <tr>
                        <td class="text-muted" style="font-size:0.8rem">${rv.res_id?.slice(0,8) || '—'}...</td>
                        <td>${rv.reader_hoten || '—'}<br><small class="text-muted">${rv.reader_so_the || ''}</small></td>
                        <td>${rv.book_title || '—'}<br><small class="text-muted">ISBN: ${rv.isbn || ''}</small></td>
                        <td>${ngaydat}</td>
                        <td>${han}</td>
                        <td><span class="badge ${badgeClass}">${badgeText}</span></td>
                        <td>${actions}</td>
                    </tr>`;
            });
        } else {
            tbody.innerHTML = '<tr><td colspan="7" style="text-align:center">Không có dữ liệu</td></tr>';
        }
        
        RenderPhanTrang('resPagination', 'resPageInfo', _trangRes, tong, HANG_RES, 'DiTrangDatCho');
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="7" style="text-align:center;color:red">Lỗi: ${err.message}</td></tr>`;
        RenderPhanTrang('resPagination', 'resPageInfo', _trangRes, 0, HANG_RES, 'DiTrangDatCho');
    }
}

// Xác nhận độc giả đã nhận sách
async function XacNhanNhan(resId) {
    if (!confirm('Xác nhận độc giả đã đến nhận sách?')) return;
    try {
        const res = await window.api.put(`/Reservation/${resId}/receive`, {});
        if (res.success) {
            alert('Xác nhận nhận sách thành công!');
            TaiDatCho();
        } else {
            alert('Lỗi: ' + (res.message || 'Không thể xác nhận'));
        }
    } catch (err) {
        alert('Lỗi: ' + err.message);
    }
}

// Huỷ đặt chỗ
async function HuyDatCho(resId) {
    if (!confirm('Xác nhận huỷ đặt chỗ này?')) return;
    try {
        const res = await window.api.put(`/Reservation/${resId}/cancel`, {});
        if (res.success) {
            alert('Huỷ đặt chỗ thành công!');
            TaiDatCho();
        } else {
            alert('Lỗi: ' + (res.message || 'Không thể huỷ'));
        }
    } catch (err) {
        alert('Lỗi: ' + err.message);
    }
}

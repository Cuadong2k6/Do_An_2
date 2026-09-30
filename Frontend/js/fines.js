// ==========================================================================
// Quản Lý Tiền Phạt (Admin)
// API Endpoints:
//   GET  /api/Fine?is_paid=&page=&pageSize=   → danh sách phiếu phạt (cần bổ sung API)
//   GET  /api/Fine/{loanId}                   → phạt theo phiếu mượn
//   POST /api/Fine/payment                    -> thu tiền phạt
//   GET  /api/Report/quahan                   -> sách quá hạn (có thông tin phạt)
// ==========================================================================

document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('fineTableBody')) {
        TaiPhat(); // mặc định: chưa thu
    }
});

let _currentFineTab = false; // false=chưa thu, true=đã thu, null=tất cả
let _trangFine = 1;
const HANG_FINE = 10;

// Tìm kiếm
function TimkiemPhat() {
    _trangFine = 1;
    TaiPhat();
}

// Chuyển tab trạng thái
function ChuyenTabPhat(isPaid) {
    _currentFineTab = isPaid;
    _trangFine = 1;
    
    ['Unpaid', 'Paid', 'All'].forEach(key => {
        const btn = document.getElementById('fineTab' + key);
        if (btn) {
            let isActive = false;
            if (key === 'All' && isPaid === null) isActive = true;
            else if (key === 'Unpaid' && isPaid === false) isActive = true;
            else if (key === 'Paid' && isPaid === true) isActive = true;
            
            btn.classList.toggle('btn-primary', isActive);
            btn.classList.toggle('btn-secondary', !isActive);
        }
    });
    
    TaiPhat();
}

// Gọi khi bấm nút số trang
function DiTrangPhat(tr) {
    _trangFine = tr;
    TaiPhat();
}

// Load danh sách phạt - dùng Report/quahan vì API Fine/list chưa có
async function TaiPhat() {
    const tbody = document.getElementById('fineTableBody');
    tbody.innerHTML = '<tr><td colspan="9" style="text-align:center">Đang tải dữ liệu...</td></tr>';
    
    try {
        const keyword = (document.getElementById('fineSearchInput')?.value || '').trim();
        
        // Dùng Report/quahan để lấy danh sách phạt (có join độc giả, phiếu mượn)
        // Sau đó lọc theo is_paid
        const res = await window.api.get('/Report/quahan');
        const allData = res.data || [];
        
        // Lọc theo tab
        let filteredData = allData;
        if (_currentFineTab !== null) {
            filteredData = allData.filter(item => {
                const isPaid = item.is_paid === 1 || item.is_paid === true;
                return isPaid === _currentFineTab;
            });
        }
        
        // Lọc theo keyword (tên độc giả, số thẻ)
        if (keyword) {
            const kw = keyword.toLowerCase();
            filteredData = filteredData.filter(item => 
                (item.reader_hoten && item.reader_hoten.toLowerCase().includes(kw)) ||
                (item.reader_so_the && item.reader_so_the.toLowerCase().includes(kw))
            );
        }
        
        // Phân trang client-side (vì API không hỗ trợ phân trang cho Report)
        const tong = filteredData.length;
        const start = (_trangFine - 1) * HANG_FINE;
        const pageData = filteredData.slice(start, start + HANG_FINE);
        
        if (pageData.length > 0) {
            tbody.innerHTML = '';
            pageData.forEach(item => {
                const ngaytao = item.loan_date ? new Date(item.loan_date).toLocaleDateString('vi-VN') : '—';
                const trangthaiThu = item.is_paid 
                    ? '<span class="badge badge-success">Đã thu</span>'
                    : '<span class="badge badge-danger">Chưa thu</span>';
                
                let actions = '—';
                if (!item.is_paid) {
                    actions = `
                        <button class="btn btn-danger" style="padding:5px 10px;font-size:0.8rem" onclick="ThuTienPhatBaoCao('${item.loan_id}')">
                            <i class="fa-solid fa-money-bill-wave"></i> Thu Tiền
                        </button>
                    `;
                } else {
                    actions = '<span class="badge badge-success">Đã hoàn tất</span>';
                }
                
                tbody.innerHTML += `
                    <tr>
                        <td class="text-muted" style="font-size:0.8rem">${item.fine_id?.slice(0,8) || item.loan_id?.slice(0,8) || '—'}...</td>
                        <td>${item.reader_hoten || '—'}<br><small class="text-muted">${item.reader_so_the || ''}</small></td>
                        <td><span class="badge badge-secondary">${item.reader_so_the || '—'}</span></td>
                        <td class="text-muted" style="font-size:0.8rem">${item.loan_id?.slice(0,8) || '—'}...</td>
                        <td style="text-align:center;color:var(--danger-color);font-weight:bold">${item.songaytre || 0} ngày</td>
                        <td style="text-align:right;font-weight:bold;color:var(--danger-color)">${(item.sotienphat || 0).toLocaleString('vi-VN')} VNĐ</td>
                        <td>${ngaytao}</td>
                        <td style="text-align:center">${trangthaiThu}</td>
                        <td>${actions}</td>
                    </tr>`;
            });
        } else {
            tbody.innerHTML = '<tr><td colspan="9" style="text-align:center">Không có dữ liệu</td></tr>';
        }
        
        RenderPhanTrang('finePagination', 'finePageInfo', _trangFine, tong, HANG_FINE, 'DiTrangPhat');
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="9" style="text-align:center;color:red">Lỗi: ${err.message}</td></tr>`;
        RenderPhanTrang('finePagination', 'finePageInfo', _trangFine, 0, HANG_FINE, 'DiTrangPhat');
    }
}

// Thu tiền phạt (dùng loan_id từ report)
async function ThuTienPhatBaoCao(loanId) {
    if (!confirm('Xác nhận thu tiền phạt cho phiếu mượn này?')) return;
    try {
        const res = await window.api.post('/Fine/payment', { loan_id: loanId });
        if (res.success) {
            alert('Thu tiền phạt thành công!');
            TaiPhat();
        } else {
            alert('Lỗi: ' + (res.message || 'Không thể thu tiền phạt'));
        }
    } catch (err) {
        alert('Lỗi: ' + err.message);
    }
}

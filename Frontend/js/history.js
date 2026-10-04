// ==========================================================================
// Lịch Sử Mượn Trả (Bạn Đọc)
// API Endpoints:
//   GET  /api/Loan/history/{readerId}        → lịch sử theo độc giả (có phân trang)
//   GET  /api/Fine/{loanId}                  → tiền phạt của 1 phiếu mượn
//   PUT  /api/Loan/{loanId}/return           → trả sách (nếu còn hạn)
//   PUT  /api/Loan/{loanId}/renew?themngay=N → gia hạn (nếu chưa quá hạn)
// ==========================================================================

document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('historyTableBody')) {
        TaiLichSu(); // mặc định load tất cả
    }
});

let _currentHistoryTab = 'all';
let _trangHistory = 1;
const HANG_HISTORY = 10;

// Lấy reader_id từ localStorage (đã login)
function LayDocGiaHienTai() {
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    return user.id;
}

// Chuyển tab
function ChuyenTabLichSu(tab) {
    _currentHistoryTab = tab;
    _trangHistory = 1;
    
    // Cập nhật UI
    ['All', 'Borrowing', 'Overdue', 'Returned'].forEach(key => {
        const btn = document.getElementById('tab' + key);
        if (btn) {
            const isActive = key.toLowerCase() === tab;
            btn.classList.toggle('btn-primary', isActive);
            btn.classList.toggle('btn-secondary', !isActive);
        }
    });
    
    TaiLichSu();
}

// Gọi khi bấm nút số trang
function DiTrangLichSu(tr) {
    _trangHistory = tr;
    TaiLichSu();
}

// Load lịch sử mượn trả
async function TaiLichSu() {
    const tbody = document.getElementById('historyTableBody');
    tbody.innerHTML = '<tr><td colspan="8" style="text-align:center">Đang tải dữ liệu...</td></tr>';
    
    const readerId = LayDocGiaHienTai();
    if (!readerId) {
        tbody.innerHTML = '<tr><td colspan="8" style="text-align:center;color:red">Không tìm thấy thông tin đăng nhập. Vui lòng đăng nhập lại.</td></tr>';
        RenderPhanTrang('historyPagination', 'historyPageInfo', _trangHistory, 0, HANG_HISTORY, 'DiTrangLichSu');
        return;
    }
    
    try {
        // Gọi API lấy lịch sử mượn theo độc giả
        const res = await window.api.get(`/Loan/history/${readerId}?page=${_trangHistory}&pageSize=${HANG_HISTORY}`);
        const tong = res.totalItems ?? 0;
        
        // Trang hiện tại vượt tổng số trang → về trang 1
        if (res.success && (!res.data || res.data.length === 0) && _trangHistory > 1) {
            _trangHistory = 1;
            return TaiLichSu();
        }
        
        if (res.success && res.data && res.data.length > 0) {
            // Lọc theo tab hiện tại
            let filteredData = res.data;
            
            if (_currentHistoryTab !== 'all') {
                filteredData = res.data.filter(loan => {
                    const daTra = !!loan.return_date;
                    const quaHan = !daTra && loan.due_date && new Date(loan.due_date) < new Date();
                    
                    if (_currentHistoryTab === 'borrowing') return !daTra && !quaHan;
                    if (_currentHistoryTab === 'overdue') return quaHan;
                    if (_currentHistoryTab === 'returned') return daTra;
                    return true;
                });
            }
            
            HienThiHangLichSu(filteredData, tbody);
        } else {
            tbody.innerHTML = '<tr><td colspan="8" style="text-align:center">Không có dữ liệu</td></tr>';
        }
        
        RenderPhanTrang('historyPagination', 'historyPageInfo', _trangHistory, tong, HANG_HISTORY, 'DiTrangLichSu');
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="8" style="text-align:center;color:red">Lỗi: ${EscapeHtml(err.message)}</td></tr>`;
        RenderPhanTrang('historyPagination', 'historyPageInfo', _trangHistory, 0, HANG_HISTORY, 'DiTrangLichSu');
    }
}

// Render các dòng lịch sử
function HienThiHangLichSu(data, tbody) {
    if (data.length > 0) {
        tbody.innerHTML = '';
        data.forEach(loan => {
            const ngaymuon  = loan.loan_date ? new Date(loan.loan_date).toLocaleDateString('vi-VN') : '—';
            const hantravue = loan.due_date  ? new Date(loan.due_date).toLocaleDateString('vi-VN')  : '—';
            const ngaytra   = loan.return_date ? new Date(loan.return_date).toLocaleDateString('vi-VN') : '—';
            
            const daTra  = !!loan.return_date;
            const quaHan = !daTra && loan.due_date && new Date(loan.due_date) < new Date();
            
            const trangthai = daTra  ? '<span class="badge badge-success">Đã trả</span>'
                           : quaHan ? '<span class="badge badge-danger">Quá hạn</span>'
                                   : '<span class="badge badge-warning">Đang mượn</span>';
            
            // Lấy thông tin sách từ loan_details (nếu có) - hiện tại API trả về list sách trong loan
            // Mặc định hiển thị "Xem chi tiết" hoặc lấy từ join fields
            const tenSach = loan.listjson_chitiet ? 'Xem chi tiết' : (loan.title || '—');
            
            // Tiền phạt
            let tienPhatHtml = '—';
            let phatAction = '';
            if (quaHan || (loan.fine_amount && loan.fine_amount > 0)) {
                tienPhatHtml = `<span style="color: var(--danger-color); font-weight: 500;">${(loan.fine_amount || 0).toLocaleString('vi-VN')} VNĐ</span>`;
                if (!loan.is_paid) {
                    phatAction = `<button class="btn btn-danger" style="padding:5px 10px;font-size:0.8rem" onclick="KiemTraPhatLichSu('${EscapeHtml(loan.loan_id)}')">Xem/Thu Phạt</button>`;
                } else {
                    phatAction = `<span class="badge badge-success">Đã thu</span>`;
                }
            }
            
            // Nút thao tác
            const nutTraSach = (!daTra && !quaHan) 
                ? `<button class="btn btn-primary" style="padding:5px 10px;font-size:0.8rem" onclick="traSachLichSu('${EscapeHtml(loan.loan_id)}')">Trả Sách</button>`
                : '';
            
            const nutGiaHan = (!daTra && !quaHan)
                ? `<button class="btn btn-secondary" style="padding:5px 10px;font-size:0.8rem" onclick="GiaHanLichSu('${EscapeHtml(loan.loan_id)}')">Gia Hạn</button>`
                : '';
            
            const actions = `${nutTraSach} ${nutGiaHan} ${phatAction}` || '—';
            
            tbody.innerHTML += `
                <tr>
                    <td class="text-muted" style="font-size:0.8rem">${EscapeHtml(loan.loan_id?.slice(0,8)) || '—'}...</td>
                    <td>${EscapeHtml(tenSach)}</td>
                    <td>${ngaymuon}</td>
                    <td>${hantravue}</td>
                    <td>${ngaytra}</td>
                    <td>${trangthai}</td>
                    <td>${tienPhatHtml}</td>
                    <td>${actions}</td>
                </tr>`;
        });
    } else {
        tbody.innerHTML = '<tr><td colspan="8" style="text-align:center">Không có dữ liệu phù hợp</td></tr>';
    }
}

// Trả sách từ lịch sử
async function traSachLichSu(loanId) {
    if (!confirm('Xác nhận trả sách cho phiếu mượn này?')) return;
    try {
        const res = await window.api.put(`/Loan/${loanId}/return`, {});
        if (res.success) {
            alert('Trả sách thành công!');
            TaiLichSu();
        } else {
            alert('Lỗi: ' + (res.message || 'Không thể trả sách'));
        }
    } catch (err) {
        alert('Lỗi: ' + err.message);
    }
}

// Gia hạn từ lịch sử
async function GiaHanLichSu(loanId) {
    const nhap = prompt('Nhập số ngày muốn gia hạn (VD: 7):', '7');
    if (nhap === null) return;
    
    const soNgay = parseInt(nhap, 10);
    if (isNaN(soNgay) || soNgay <= 0) {
        alert('Số ngày gia hạn phải là số nguyên lớn hơn 0.');
        return;
    }
    
    try {
        const res = await window.api.put(`/Loan/${loanId}/renew?themngay=${soNgay}`, {});
        if (res.success) {
            alert(res.message || 'Gia hạn phiếu mượn thành công!');
            TaiLichSu();
        } else {
            alert('Không thể gia hạn: ' + (res.message || 'lỗi không xác định'));
        }
    } catch (err) {
        alert('Không thể gia hạn: ' + err.message);
    }
}

// Kiểm tra tiền phạt từ lịch sử
async function KiemTraPhatLichSu(loanId) {
    try {
        const res = await window.api.get(`/Fine/${loanId}`);
        if (res.success && res.data) {
            const f = res.data;
            const soTien = (f.fine_amount || 0).toLocaleString('vi-VN') + ' VNĐ';
            const daThanhToan = f.payment_date ? 'Đã thanh toán' : 'Chưa thanh toán';
            
            if (!f.payment_date) {
                if (confirm(`Tiền phạt: ${soTien}\nTrạng thái: ${daThanhToan}\n\nBấm OK để thu tiền phạt ngay.`)) {
                    ThuTienPhatLichSu(f.fine_id);
                }
            } else {
                alert(`Tiền phạt: ${soTien}\nTrạng thái: ${daThanhToan}`);
            }
        } else {
            alert('Phiếu mượn này chưa có tiền phạt.');
        }
    } catch (err) {
        alert('Lỗi: ' + err.message);
    }
}

// Thu tiền phạt từ lịch sử
async function ThuTienPhatLichSu(fineId) {
    try {
        const res = await window.api.post('/Fine/payment', { fine_id: fineId });
        if (res.success) {
            alert('Thu tiền phạt thành công!');
            TaiLichSu();
        } else {
            alert('Lỗi: ' + (res.message || 'Không thể thu tiền phạt'));
        }
    } catch (err) {
        alert('Lỗi: ' + err.message);
    }
}

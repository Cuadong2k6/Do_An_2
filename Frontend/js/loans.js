// ==========================================================================
// Quản lý Mượn Trả (Admin)
// API Endpoints:
//   GET  /api/Loan/active                    → phiếu đang mượn (chưa trả, chưa quá hạn)
//   GET  /api/Loan/overdue                   → phiếu quá hạn
//   GET  /api/Loan/history/{readerId}        → lịch sử theo độc giả
//   POST /api/Loan                           → tạo phiếu  body: { reader_id, copy_ids[], due_date }
//   PUT  /api/Loan/{loanId}/return           → trả sách
//   PUT  /api/Loan/{loanId}/renew?themngay=N → gia hạn phiếu
//   GET  /api/Fine/{loanId}                  → tiền phạt
//   POST /api/Fine/payment                   → thu tiền phạt
// ==========================================================================

document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('loanTableBody')) {
        TaiPhieuDangMuon(); // mặc định: danh sách phiếu đang mượn

        const loanForm = document.getElementById('loanForm');
        if (loanForm) {
            loanForm.addEventListener('submit', XuLyTaoPhieuMuon);
        }

        document.getElementById('btnTabActive')?.addEventListener('click', () => ChuyenTab('active'));
        document.getElementById('btnTabOverdue')?.addEventListener('click', () => ChuyenTab('overdue'));
        document.getElementById('btnTabHistory')?.addEventListener('click', () => ChuyenTab('history'));
    }
});

let _currentTab = 'active';
let _keywordPhieu = '';
const HANG_PHIEU = 10;        // 10 hàng/trang
let _trangPhieu = 1;
let _lastHistoryReaderId = null;

// Tìm kiếm phiếu mượn từ ô header — áp cho tab Đang Mượn / Quá Hạn
// (tab Lịch sử đã lọc theo từng Độc Giả riêng nên bỏ qua ô tìm này)
function TimkiemPhieuMuon(value) {
    _keywordPhieu = (value || '').trim();
    _trangPhieu = 1;
    if (_currentTab === 'overdue') TaiPhieuQuaHan();
    else if (_currentTab === 'history') return;
    else TaiPhieuDangMuon();
}

// Gọi khi bấm nút số trang
function DiTrangPhieu(tr) {
    _trangPhieu = tr;
    if (_currentTab === 'overdue') TaiPhieuQuaHan();
    else if (_currentTab === 'history') {
        if (_lastHistoryReaderId) TaiLichSuMuon(_lastHistoryReaderId);
    } else TaiPhieuDangMuon();
}

function ThietLapTabUI(tab) {
    ['Active', 'Overdue', 'History'].forEach(key => {
        const btn = document.getElementById('btnTab' + key);
        if (btn) {
            const isActive = key.toLowerCase() === tab;
            btn.classList.toggle('btn-primary', isActive);
            btn.classList.toggle('btn-secondary', !isActive);
        }
    });
}

function ChuyenTab(tab) {
    _currentTab = tab;
    _trangPhieu = 1;
    ThietLapTabUI(tab);

    if (tab === 'active') {
        TaiPhieuDangMuon();
    } else if (tab === 'overdue') {
        TaiPhieuQuaHan();
    } else {
        MoModalChonDocGiaLichSu();
    }
}

function MoModalChonDocGiaLichSu() {
    const modal = document.createElement('div');
    modal.id = 'historyReaderModal';
    modal.style.cssText = 'display:flex;position:fixed;z-index:1050;inset:0;background:rgba(0,0,0,0.5);align-items:center;justify-content:center;';
    modal.innerHTML = `
        <div style="background:#fff;padding:25px;border-radius:8px;max-width:400px;width:90%;box-shadow:0 4px 20px rgba(0,0,0,0.15);">
            <h3 style="margin-top:0;color:var(--primary-color);">Xem Lịch Sử Mượn Theo Độc Giả</h3>
            <p style="color:var(--text-muted);margin-bottom:20px;">Nhập ID (GUID) của độc giả để xem lịch sử mượn trả.</p>
            <div style="margin-bottom:16px;">
                <input type="text" id="historyReaderIdInput" class="form-control" 
                       placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" 
                       style="width:100%;padding:10px;box-sizing:border-box;border:1px solid var(--border-color);border-radius:4px;">
            </div>
            <div id="historyReaderError" style="color:#dc2626;font-size:0.85rem;margin-bottom:12px;display:none;"></div>
            <div style="display:flex;justify-content:flex-end;gap:10px;">
                <button type="button" class="btn btn-secondary" onclick="DongModalChonDocGia()">Hủy</button>
                <button type="button" class="btn btn-primary" onclick="XacNhanChonDocGiaLichSu()">Xem Lịch Sử</button>
            </div>
        </div>
    `;
    document.body.appendChild(modal);
    document.getElementById('historyReaderIdInput').focus();
    document.getElementById('historyReaderIdInput').addEventListener('keydown', e => {
        if (e.key === 'Enter') XacNhanChonDocGiaLichSu();
    });
}

function DongModalChonDocGia() {
    const modal = document.getElementById('historyReaderModal');
    if (modal) modal.remove();
    
    // Nếu hủy thì quay về tab Đang Mượn
    _currentTab = 'active';
    ThietLapTabUI('active');
    TaiPhieuDangMuon();
}

function XacNhanChonDocGiaLichSu() {
    const readerId = document.getElementById('historyReaderIdInput').value.trim();
    const errorDiv = document.getElementById('historyReaderError');
    
    if (!readerId) {
        errorDiv.textContent = 'Vui lòng nhập ID độc giả.';
        errorDiv.style.display = 'block';
        return;
    }
    
    try {
        // Validate GUID format
        const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
        if (!guidRegex.test(readerId)) {
            errorDiv.textContent = 'Định dạng GUID không hợp lệ.';
            errorDiv.style.display = 'block';
            return;
        }
        
        DongModalChonDocGia();
        _lastHistoryReaderId = readerId;
        TaiLichSuMuon(readerId);
    } catch (e) {
        errorDiv.textContent = 'Lỗi: ' + e.message;
        errorDiv.style.display = 'block';
    }
}

async function TaiPhieuDangMuon() {
    const tbody = document.getElementById('loanTableBody');
    tbody.innerHTML = '<tr><td colspan="6" style="text-align:center">Đang tải dữ liệu...</td></tr>';
    try {
        const p = new URLSearchParams({ page: _trangPhieu, pageSize: HANG_PHIEU });
        if (_keywordPhieu) p.set('keyword', _keywordPhieu);
        const res = await window.api.get(`/Loan/active?${p.toString()}`);
        if (res.success && (!res.data || res.data.length === 0) && _trangPhieu > 1) {
            _trangPhieu = 1; // trang vượt tổng → về trang 1
            return TaiPhieuDangMuon();
        }
        HienThiHangPhieu(res, tbody, true);
        renderphantrang('loanPagination', 'loanPageInfo', _trangPhieu, res.totalItems ?? 0, HANG_PHIEU, 'DiTrangPhieu');
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="6" style="text-align:center;color:red">Lỗi: ${err.message}</td></tr>`;
        renderphantrang('loanPagination', 'loanPageInfo', _trangPhieu, 0, HANG_PHIEU, 'DiTrangPhieu');
    }
}

async function TaiPhieuQuaHan() {
    const tbody = document.getElementById('loanTableBody');
    tbody.innerHTML = '<tr><td colspan="6" style="text-align:center">Đang tải dữ liệu...</td></tr>';
    try {
        const p = new URLSearchParams({ page: _trangPhieu, pageSize: HANG_PHIEU });
        if (_keywordPhieu) p.set('keyword', _keywordPhieu);
        const res = await window.api.get(`/Loan/overdue?${p.toString()}`);
        if (res.success && (!res.data || res.data.length === 0) && _trangPhieu > 1) {
            _trangPhieu = 1;
            return TaiPhieuQuaHan();
        }
        HienThiHangPhieu(res, tbody, true);
        renderphantrang('loanPagination', 'loanPageInfo', _trangPhieu, res.totalItems ?? 0, HANG_PHIEU, 'DiTrangPhieu');
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="6" style="text-align:center;color:red">Lỗi: ${err.message}</td></tr>`;
        renderphantrang('loanPagination', 'loanPageInfo', _trangPhieu, 0, HANG_PHIEU, 'DiTrangPhieu');
    }
}

async function TaiLichSuMuon(readerId) {
    _lastHistoryReaderId = readerId;
    const tbody = document.getElementById('loanTableBody');
    tbody.innerHTML = '<tr><td colspan="6" style="text-align:center">Đang tải dữ liệu...</td></tr>';
    try {
        const res = await window.api.get(`/Loan/history/${readerId}?page=${_trangPhieu}&pageSize=${HANG_PHIEU}`);
        if (res.success && (!res.data || res.data.length === 0) && _trangPhieu > 1) {
            _trangPhieu = 1;
            return TaiLichSuMuon(readerId);
        }
        // Lịch sử: vẫn hiện nút với phiếu CHƯA trả (để trả/gia hạn từ đây)
        HienThiHangPhieu(res, tbody, true);
        renderphantrang('loanPagination', 'loanPageInfo', _trangPhieu, res.totalItems ?? 0, HANG_PHIEU, 'DiTrangPhieu');
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="6" style="text-align:center;color:red">Lỗi: ${err.message}</td></tr>`;
        renderphantrang('loanPagination', 'loanPageInfo', _trangPhieu, 0, HANG_PHIEU, 'DiTrangPhieu');
    }
}

function HienThiHangPhieu(res, tbody, showActions) {
    if (res.success && res.data && res.data.length > 0) {
        tbody.innerHTML = '';
        res.data.forEach(loan => {
            const ngaymuon  = loan.loan_date ? new Date(loan.loan_date).toLocaleDateString('vi-VN') : '—';
            const hantravue = loan.due_date  ? new Date(loan.due_date).toLocaleDateString('vi-VN')  : '—';

            const daTra  = !!loan.return_date;
            const quaHan = !daTra && loan.due_date && new Date(loan.due_date) < new Date();

            const trangthai = daTra  ? '<span class="badge badge-success">Đã trả</span>'
                           : quaHan ? '<span class="badge badge-danger">Quá hạn</span>'
                                    : '<span class="badge badge-warning">Đang mượn</span>';

            // Logic nút:
            //   - Đã trả      → không thao tác
            //   - Quá hạn     → chỉ Trả sách / Xem phạt (SP chặn gia hạn phiếu quá hạn)
            //   - Còn hạn     → thêm nút Gia hạn
            const nutGiaHan = (!daTra && !quaHan)
                ? `<button class="btn btn-secondary" style="padding:5px 10px;font-size:0.8rem"
                        onclick="GiaHanPhieu('${loan.loan_id}')">Gia Hạn</button>`
                : '';
            const actions = showActions && !daTra ? `
                ${nutGiaHan}
                <button class="btn btn-primary" style="padding:5px 10px;font-size:0.8rem"
                    onclick="TraSach('${loan.loan_id}')">Trả Sách</button>
                <button class="btn btn-warning" style="padding:5px 10px;font-size:0.8rem"
                    onclick="KiemTraPhat('${loan.loan_id}')">Xem Phạt</button>` : '—';

            tbody.innerHTML += `
                <tr>
                    <td class="text-muted" style="font-size:0.8rem">${loan.loan_id?.slice(0,8) || '—'}...</td>
                    <td>${loan.reader_hoten || loan.reader_id || '—'}</td>
                    <td>${ngaymuon}</td>
                    <td>${hantravue}</td>
                    <td>${trangthai}</td>
                    <td>${actions}</td>
                </tr>`;
        });
    } else {
        tbody.innerHTML = '<tr><td colspan="6" style="text-align:center">Không có dữ liệu</td></tr>';
    }
}

// --- Gia hạn phiếu mượn (sửa ngày trả dự kiến) ---
async function GiaHanPhieu(loanId) {
    const nhap = prompt('Nhập số ngày muốn gia hạn (VD: 7):', '7');
    if (nhap === null) return; // bấm Hủy

    const soNgay = parseInt(nhap, 10);
    if (isNaN(soNgay) || soNgay <= 0) {
        alert('Số ngày gia hạn phải là số nguyên lớn hơn 0.');
        return;
    }

    try {
        const res = await window.api.put(`/Loan/${loanId}/renew?themngay=${soNgay}`, {});
        if (res.success) {
            alert(res.message || 'Gia hạn phiếu mượn thành công!');
            if (_currentTab === 'overdue') TaiPhieuQuaHan();
            else TaiPhieuDangMuon();
        } else {
            alert('Không thể gia hạn: ' + (res.message || 'lỗi không xác định'));
        }
    } catch (err) {
        alert('Không thể gia hạn: ' + err.message);
    }
}

// --- Trả Sách ---
async function TraSach(loanId) {
    if (!confirm('Xác nhận trả sách cho phiếu mượn này?')) return;
    try {
        const res = await window.api.put(`/Loan/${loanId}/return`, {});
        if (res.success) {
            alert('Trả sách thành công!');
            if (_currentTab === 'history') TaiLichSuMuon(_lastHistoryReaderId);
            else if (_currentTab === 'overdue') TaiPhieuQuaHan();
            else TaiPhieuDangMuon();
        } else {
            alert('Lỗi: ' + (res.message || 'Không thể trả sách'));
        }
    } catch (err) {
        alert('Lỗi: ' + err.message);
    }
}

// --- Kiểm tra tiền phạt ---
async function KiemTraPhat(loanId) {
    try {
        const res = await window.api.get(`/Fine/${loanId}`);
        if (res.success && res.data) {
            const f = res.data;
            const soTien = (f.fine_amount || 0).toLocaleString('vi-VN') + ' VNĐ';
            const daThanhToan = f.payment_date ? 'Đã thanh toán' : 'Chưa thanh toán';

            if (!f.payment_date) {
                if (confirm(`Tiền phạt: ${soTien}\nTrạng thái: ${daThanhToan}\n\nBấm OK để thu tiền phạt ngay.`)) {
                    ThuTienPhat(f.fine_id);
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

// --- Thu tiền phạt ---
async function ThuTienPhat(fineId) {
    try {
        const res = await window.api.post('/Fine/payment', { fine_id: fineId });
        if (res.success) {
            alert('Thu tiền phạt thành công!');
            if (_currentTab === 'history') TaiLichSuMuon(_lastHistoryReaderId);
            else TaiPhieuQuaHan();
        } else {
            alert('Lỗi: ' + (res.message || 'Không thể thu tiền phạt'));
        }
    } catch (err) {
        alert('Lỗi: ' + err.message);
    }
}

// --- Tạo phiếu mượn ---
function MoModalTaoPhieu() {
    document.getElementById('loanForm').reset();
    document.getElementById('loanModalError').style.display = 'none';
    document.getElementById('loanModal').style.display = 'block';
}

function DongModalPhieu() {
    document.getElementById('loanModal').style.display = 'none';
}

async function XuLyTaoPhieuMuon(e) {
    e.preventDefault();
    const errorDiv = document.getElementById('loanModalError');
    const btn = document.getElementById('btnSaveLoan');
    errorDiv.style.display = 'none';
    btn.disabled = true;
    btn.innerHTML = 'Đang tạo...';

    const readerId  = document.getElementById('loanReaderId').value.trim();
    const copyIdsRaw = document.getElementById('loanCopyIds').value.trim();
    const dueDate   = document.getElementById('loanDueDate').value;

    // copy_ids là danh sách Guid, mỗi dòng 1 ID
    const copyIds = copyIdsRaw.split('\n').map(s => s.trim()).filter(s => s.length > 0);

    if (!readerId || copyIds.length === 0 || !dueDate) {
        errorDiv.textContent = 'Vui lòng nhập đầy đủ thông tin.';
        errorDiv.style.display = 'block';
        btn.disabled = false;
        btn.innerHTML = 'Tạo Phiếu';
        return;
    }

    try {
        const res = await window.api.post('/Loan', {
            reader_id: readerId,
            copy_ids:  copyIds,
            due_date:  new Date(dueDate).toISOString()
        });

        if (res.success) {
            alert('Tạo phiếu mượn thành công!');
            DongModalPhieu();
            TaiPhieuDangMuon();
        } else {
            throw new Error(res.message || 'Lỗi khi tạo phiếu mượn.');
        }
    } catch (err) {
        errorDiv.textContent = err.message;
        errorDiv.style.display = 'block';
    } finally {
        btn.disabled = false;
        btn.innerHTML = 'Tạo Phiếu';
    }
}

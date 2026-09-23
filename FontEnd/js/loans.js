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
        loadActiveLoans(); // mặc định: danh sách phiếu đang mượn

        const loanForm = document.getElementById('loanForm');
        if (loanForm) {
            loanForm.addEventListener('submit', handleTaoPhieuMuon);
        }

        document.getElementById('btnTabActive')?.addEventListener('click', () => switchTab('active'));
        document.getElementById('btnTabOverdue')?.addEventListener('click', () => switchTab('overdue'));
        document.getElementById('btnTabHistory')?.addEventListener('click', () => switchTab('history'));
    }
});

let _currentTab = 'active';
let _keywordPhieu = '';
const HANG_PHIEU = 10;        // 10 hàng/trang
let _trangPhieu = 1;
let _lastHistoryReaderId = null;

// Tìm kiếm phiếu mượn từ ô header — áp cho tab Đang Mượn / Quá Hạn
// (tab Lịch sử đã lọc theo từng Độc Giả riêng nên bỏ qua ô tìm này)
function timkiemPhieuMuon(value) {
    _keywordPhieu = (value || '').trim();
    _trangPhieu = 1;
    if (_currentTab === 'overdue') loadOverdueLoans();
    else if (_currentTab === 'history') return;
    else loadActiveLoans();
}

// Gọi khi bấm nút số trang
function diTrangPhieu(tr) {
    _trangPhieu = tr;
    if (_currentTab === 'overdue') loadOverdueLoans();
    else if (_currentTab === 'history') {
        if (_lastHistoryReaderId) loadLoanHistory(_lastHistoryReaderId);
    } else loadActiveLoans();
}

function setTabUI(tab) {
    ['Active', 'Overdue', 'History'].forEach(key => {
        const btn = document.getElementById('btnTab' + key);
        if (btn) {
            const isActive = key.toLowerCase() === tab;
            btn.classList.toggle('btn-primary', isActive);
            btn.classList.toggle('btn-secondary', !isActive);
        }
    });
}

function switchTab(tab) {
    _currentTab = tab;
    _trangPhieu = 1;
    setTabUI(tab);

    if (tab === 'active') {
        loadActiveLoans();
    } else if (tab === 'overdue') {
        loadOverdueLoans();
    } else {
        const readerId = prompt('Nhập ID độc giả để xem lịch sử mượn:');
        if (readerId) {
            loadLoanHistory(readerId);
        } else {
            // Hủy nhập → quay về tab Đang Mượn
            _currentTab = 'active';
            setTabUI('active');
            loadActiveLoans();
        }
    }
}

async function loadActiveLoans() {
    const tbody = document.getElementById('loanTableBody');
    tbody.innerHTML = '<tr><td colspan="6" style="text-align:center">Đang tải dữ liệu...</td></tr>';
    try {
        const p = new URLSearchParams({ page: _trangPhieu, pageSize: HANG_PHIEU });
        if (_keywordPhieu) p.set('keyword', _keywordPhieu);
        const res = await window.api.get(`/Loan/active?${p.toString()}`);
        if (res.success && (!res.data || res.data.length === 0) && _trangPhieu > 1) {
            _trangPhieu = 1; // trang vượt tổng → về trang 1
            return loadActiveLoans();
        }
        renderLoanRows(res, tbody, true);
        renderphantrang('loanPagination', 'loanPageInfo', _trangPhieu, res.totalItems ?? 0, HANG_PHIEU, 'diTrangPhieu');
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="6" style="text-align:center;color:red">Lỗi: ${err.message}</td></tr>`;
        renderphantrang('loanPagination', 'loanPageInfo', _trangPhieu, 0, HANG_PHIEU, 'diTrangPhieu');
    }
}

async function loadOverdueLoans() {
    const tbody = document.getElementById('loanTableBody');
    tbody.innerHTML = '<tr><td colspan="6" style="text-align:center">Đang tải dữ liệu...</td></tr>';
    try {
        const p = new URLSearchParams({ page: _trangPhieu, pageSize: HANG_PHIEU });
        if (_keywordPhieu) p.set('keyword', _keywordPhieu);
        const res = await window.api.get(`/Loan/overdue?${p.toString()}`);
        if (res.success && (!res.data || res.data.length === 0) && _trangPhieu > 1) {
            _trangPhieu = 1;
            return loadOverdueLoans();
        }
        renderLoanRows(res, tbody, true);
        renderphantrang('loanPagination', 'loanPageInfo', _trangPhieu, res.totalItems ?? 0, HANG_PHIEU, 'diTrangPhieu');
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="6" style="text-align:center;color:red">Lỗi: ${err.message}</td></tr>`;
        renderphantrang('loanPagination', 'loanPageInfo', _trangPhieu, 0, HANG_PHIEU, 'diTrangPhieu');
    }
}

async function loadLoanHistory(readerId) {
    _lastHistoryReaderId = readerId;
    const tbody = document.getElementById('loanTableBody');
    tbody.innerHTML = '<tr><td colspan="6" style="text-align:center">Đang tải dữ liệu...</td></tr>';
    try {
        const res = await window.api.get(`/Loan/history/${readerId}?page=${_trangPhieu}&pageSize=${HANG_PHIEU}`);
        if (res.success && (!res.data || res.data.length === 0) && _trangPhieu > 1) {
            _trangPhieu = 1;
            return loadLoanHistory(readerId);
        }
        // Lịch sử: vẫn hiện nút với phiếu CHƯA trả (để trả/gia hạn từ đây)
        renderLoanRows(res, tbody, true);
        renderphantrang('loanPagination', 'loanPageInfo', _trangPhieu, res.totalItems ?? 0, HANG_PHIEU, 'diTrangPhieu');
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="6" style="text-align:center;color:red">Lỗi: ${err.message}</td></tr>`;
        renderphantrang('loanPagination', 'loanPageInfo', _trangPhieu, 0, HANG_PHIEU, 'diTrangPhieu');
    }
}

function renderLoanRows(res, tbody, showActions) {
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
                        onclick="giathanphieu('${loan.loan_id}')">Gia Hạn</button>`
                : '';
            const actions = showActions && !daTra ? `
                ${nutGiaHan}
                <button class="btn btn-primary" style="padding:5px 10px;font-size:0.8rem"
                    onclick="traSach('${loan.loan_id}')">Trả Sách</button>
                <button class="btn btn-warning" style="padding:5px 10px;font-size:0.8rem"
                    onclick="kiemTraPhat('${loan.loan_id}')">Xem Phạt</button>` : '—';

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
async function giathanphieu(loanId) {
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
            if (_currentTab === 'overdue') loadOverdueLoans();
            else loadActiveLoans();
        } else {
            alert('Không thể gia hạn: ' + (res.message || 'lỗi không xác định'));
        }
    } catch (err) {
        alert('Không thể gia hạn: ' + err.message);
    }
}

// --- Trả Sách ---
async function traSach(loanId) {
    if (!confirm('Xác nhận trả sách cho phiếu mượn này?')) return;
    try {
        const res = await window.api.put(`/Loan/${loanId}/return`, {});
        if (res.success) {
            alert('Trả sách thành công!');
            if (_currentTab === 'history') loadLoanHistory(_lastHistoryReaderId);
            else if (_currentTab === 'overdue') loadOverdueLoans();
            else loadActiveLoans();
        } else {
            alert('Lỗi: ' + (res.message || 'Không thể trả sách'));
        }
    } catch (err) {
        alert('Lỗi: ' + err.message);
    }
}

// --- Kiểm tra tiền phạt ---
async function kiemTraPhat(loanId) {
    try {
        const res = await window.api.get(`/Fine/${loanId}`);
        if (res.success && res.data) {
            const f = res.data;
            const soTien = (f.fine_amount || 0).toLocaleString('vi-VN') + ' VNĐ';
            const daThanhToan = f.payment_date ? 'Đã thanh toán' : 'Chưa thanh toán';

            if (!f.payment_date) {
                if (confirm(`Tiền phạt: ${soTien}\nTrạng thái: ${daThanhToan}\n\nBấm OK để thu tiền phạt ngay.`)) {
                    thuTienPhat(f.fine_id);
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
async function thuTienPhat(fineId) {
    try {
        const res = await window.api.post('/Fine/payment', { fine_id: fineId });
        if (res.success) {
            alert('Thu tiền phạt thành công!');
            if (_currentTab === 'history') loadLoanHistory(_lastHistoryReaderId);
            else loadOverdueLoans();
        } else {
            alert('Lỗi: ' + (res.message || 'Không thể thu tiền phạt'));
        }
    } catch (err) {
        alert('Lỗi: ' + err.message);
    }
}

// --- Tạo phiếu mượn ---
function moModalTaoPhieu() {
    document.getElementById('loanForm').reset();
    document.getElementById('loanModalError').style.display = 'none';
    document.getElementById('loanModal').style.display = 'block';
}

function dongModalLoan() {
    document.getElementById('loanModal').style.display = 'none';
}

async function handleTaoPhieuMuon(e) {
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
            dongModalLoan();
            loadActiveLoans();
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

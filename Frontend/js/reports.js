// ==========================================================================
// Báo Cáo Thống Kê (Admin)
// API Endpoints:
//   GET /api/Report/sachmuonnhieu?topN=10     → sách mượn nhiều nhất
//   GET /api/Report/tonkho                    → tồn kho theo thể loại
//   GET /api/Report/quahan                    → sách quá hạn chưa trả
// ==========================================================================

document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('reportSachMuonBody')) {
        TaiTatCaBaoCao();
    }
});

let _currentReportTab = 'sachmuonnhieu';

// Chuyển tab báo cáo
function ChuyenTabBaoCao(tab) {
    _currentReportTab = tab;
    
    // Ẩn/hiện nội dung báo cáo
    document.querySelectorAll('.report-content').forEach(el => {
        el.style.display = 'none';
    });
    document.getElementById('report' + VietHoaChuDau(tab)).style.display = 'block';
    
    // Cập nhật UI tab
    ['SachMuon', 'TonKho', 'QuaHan'].forEach(key => {
        const btn = document.getElementById('reportTab' + key);
        if (btn) {
            const isActive = key.toLowerCase() === tab;
            btn.classList.toggle('btn-primary', isActive);
            btn.classList.toggle('btn-secondary', !isActive);
        }
    });
}

function VietHoaChuDau(str) {
    return str.charAt(0).toUpperCase() + str.slice(1);
}

// Load tất cả báo cáo
async function TaiTatCaBaoCao() {
    await Promise.all([
        TaiSachMuonNhieu(),
        TaiTonKho(),
        TaiQuaHan()
    ]);
}

// 1. Sách mượn nhiều nhất
async function TaiSachMuonNhieu() {
    const tbody = document.getElementById('reportSachMuonBody');
    tbody.innerHTML = '<tr><td colspan="5" style="text-align:center">Đang tải dữ liệu...</td></tr>';
    
    try {
        const res = await window.api.get('/Report/sachmuonnhieu?topN=20');
        if (res.success && res.data && res.data.length > 0) {
            tbody.innerHTML = '';
            res.data.forEach((item, index) => {
                tbody.innerHTML += `
                    <tr>
                        <td style="text-align:center">${index + 1}</td>
                        <td style="font-weight:500">${item.title}</td>
                        <td>${item.tacgia || '—'}</td>
                        <td>${item.theloai || '—'}</td>
                        <td style="text-align:center;font-weight:bold;color:var(--primary-color)">${item.sotluongmuon || 0}</td>
                    </tr>`;
            });
        } else {
            tbody.innerHTML = '<tr><td colspan="5" style="text-align:center">Không có dữ liệu</td></tr>';
        }
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="5" style="text-align:center;color:red">Lỗi: ${err.message}</td></tr>`;
    }
}

// 2. Tồn kho theo thể loại
async function TaiTonKho() {
    const tbody = document.getElementById('reportTonKhoBody');
    tbody.innerHTML = '<tr><td colspan="5" style="text-align:center">Đang tải dữ liệu...</td></tr>';
    
    try {
        const res = await window.api.get('/Report/tonkho');
        if (res.success && res.data && res.data.length > 0) {
            tbody.innerHTML = '';
            res.data.forEach(item => {
                tbody.innerHTML += `
                    <tr>
                        <td style="font-weight:500">${item.theloai || 'Chưa phân loại'}</td>
                        <td style="text-align:center">${item.sodausach || 0}</td>
                        <td style="text-align:center">${item.tongbancao || 0}</td>
                        <td style="text-align:center;color:var(--secondary-color);font-weight:500">${item.bansangio || 0}</td>
                        <td style="text-align:center;color:var(--warning-color);font-weight:500">${item.danmuon || 0}</td>
                    </tr>`;
            });
        } else {
            tbody.innerHTML = '<tr><td colspan="5" style="text-align:center">Không có dữ liệu</td></tr>';
        }
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="5" style="text-align:center;color:red">Lỗi: ${err.message}</td></tr>`;
    }
}

// 3. Sách quá hạn
async function TaiQuaHan() {
    const tbody = document.getElementById('reportQuaHanBody');
    tbody.innerHTML = '<tr><td colspan="9" style="text-align:center">Đang tải dữ liệu...</td></tr>';
    
    try {
        const res = await window.api.get('/Report/quahan');
        if (res.success && res.data && res.data.length > 0) {
            tbody.innerHTML = '';
            res.data.forEach(item => {
                const trangthaiThu = item.is_paid 
                    ? '<span class="badge badge-success">Đã thu</span>'
                    : '<span class="badge badge-danger">Chưa thu</span>';
                
                tbody.innerHTML += `
                    <tr>
                        <td class="text-muted" style="font-size:0.8rem">${item.loan_id?.slice(0,8) || '—'}...</td>
                        <td>${item.reader_hoten || '—'}</td>
                        <td><span class="badge badge-secondary">${item.reader_so_the || '—'}</span></td>
                        <td>${item.reader_sodienthoai || '—'}</td>
                        <td>${item.loan_date ? new Date(item.loan_date).toLocaleDateString('vi-VN') : '—'}</td>
                        <td>${item.due_date ? new Date(item.due_date).toLocaleDateString('vi-VN') : '—'}</td>
                        <td style="text-align:center;color:var(--danger-color);font-weight:bold">${item.songaytre || 0} ngày</td>
                        <td style="text-align:right;font-weight:bold;color:var(--danger-color)">${(item.sotienphat || 0).toLocaleString('vi-VN')} VNĐ</td>
                        <td style="text-align:center">${trangthaiThu}</td>
                    </tr>`;
            });
        } else {
            tbody.innerHTML = '<tr><td colspan="9" style="text-align:center">Không có phiếu quá hạn</td></tr>';
        }
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="9" style="text-align:center;color:red">Lỗi: ${err.message}</td></tr>`;
    }
}

// Xuất báo cáo Excel (placeholder - cần backend hỗ trợ)
function exportReport(type) {
    const typeNames = {
        'sachmuonnhieu': 'Sách Mượn Nhiều Nhất',
        'tonkho': 'Tồn Kho Theo Thể Loại',
        'quahan': 'Sách Quá Hạn'
    };
    alert(`Chức năng xuất Excel "${typeNames[type]}" đang phát triển.\nCần backend hỗ trợ endpoint export file.`);
}

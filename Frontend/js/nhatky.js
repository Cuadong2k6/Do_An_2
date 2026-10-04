// ==========================================================================
// Nhật Ký Thay Đổi (Admin) — xem lại dấu vết khi sửa / xoá sách, bạn đọc
// API: GET /api/Nhatky?keyword=&bang=&hanhdong=&page=&pageSize=
// Chỉ đọc. Không có nút khôi phục — muốn sửa lại thì lấy giá trị cột "cũ" nhập tay.
// ==========================================================================

const HANG_NHATKY = 20;      // 20 dòng/trang
let _trangNhatky = 1;

document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('nhatkyTableBody')) {
        TaiNhatky();
    }
});

// Gọi khi người dùng gõ ô tìm kiếm / đổi bộ lọc → về trang 1
function TimkiemNhatky() {
    _trangNhatky = 1;
    TaiNhatky();
}

function DiTrangNhatky(tr) {
    _trangNhatky = tr;
    TaiNhatky();
}

// Ô nhập "rỗng" hiển thị (rỗng) — trong nhật ký thường là sửa thành rỗng, cần phân biệt
function GiaTriNhatky(v) {
    if (v === null || v === undefined || String(v).trim() === '') return '(rỗng)';
    return String(v);
}

async function TaiNhatky() {
    const tbody = document.getElementById('nhatkyTableBody');
    tbody.innerHTML = '<tr><td colspan="6" style="text-align:center">Đang tải dữ liệu...</td></tr>';

    try {
        const keyword  = (document.getElementById('nhatkyKeyword')?.value || '').trim();
        const bang     = document.getElementById('filterNhatkyBang')?.value ?? '';
        const hanhdong = document.getElementById('filterNhatkyHanhDong')?.value ?? '';

        const params = new URLSearchParams({ page: _trangNhatky, pageSize: HANG_NHATKY });
        if (keyword !== '')  params.set('keyword', keyword);
        if (bang !== '')     params.set('bang', bang);
        if (hanhdong !== '') params.set('hanhdong', hanhdong);

        const res  = await window.api.get(`/Nhatky?${params.toString()}`);
        const tong = res.totalItems ?? 0;

        // Xoá hết sạch ở trang cuối → về trang 1
        if (res.success && (!res.data || res.data.length === 0) && _trangNhatky > 1) {
            _trangNhatky = 1;
            return TaiNhatky();
        }

        if (res.success && res.data && res.data.length > 0) {
            tbody.innerHTML = '';
            res.data.forEach(n => {
                const laXoa = n.hanhdong === 'XOA';

                const badgeBang = n.bang === 'books'
                    ? '<span class="badge badge-secondary">Sách</span>'
                    : '<span class="badge badge-secondary">Độc Giả</span>';

                const badgeHanhDong = laXoa
                    ? '<span class="badge badge-danger">Xoá</span>'
                    : '<span class="badge badge-warning">Sửa</span>';

                // Dòng xoá: không có cột nào cụ thể nên hiện nội dung gộp vào "trường"
                const oTruong = laXoa
                    ? '<span class="text-muted">toàn bộ bản ghi</span>'
                    : `<strong>${EscapeHtml(n.truong || '(không rõ)')}</strong>`;

                const oCu = laXoa
                    ? `<span class="text-muted">${EscapeHtml(GiaTriNhatky(n.truoc))}</span>`
                    : `<span style="color:#dc2626">${EscapeHtml(GiaTriNhatky(n.truoc))}</span>`;

                const oMoi = laXoa
                    ? '<span class="text-muted">—</span>'
                    : `<span style="color:#16a34a">${EscapeHtml(GiaTriNhatky(n.sau))}</span>`;

                tbody.innerHTML += `
                    <tr>
                        <td style="white-space:nowrap">${new Date(n.thoigian).toLocaleString('vi-VN')}</td>
                        <td>${badgeBang}</td>
                        <td><span class="badge badge-secondary">${EscapeHtml(n.doituong)}</span></td>
                        <td>${badgeHanhDong}</td>
                        <td>${oTruong}</td>
                        <td style="max-width:260px;word-break:break-word">
                            ${oCu}
                            ${laXoa ? '' : '<i class="fa-solid fa-arrow-right-long text-muted" style="font-size:0.7rem"></i>'}
                            ${oMoi}
                        </td>
                    </tr>`;
            });
        } else {
            tbody.innerHTML = '<tr><td colspan="6" style="text-align:center">Chưa có thay đổi nào được ghi nhận</td></tr>';
        }

        RenderPhanTrang('nhatkyPagination', 'nhatkyPageInfo', _trangNhatky, tong, HANG_NHATKY, 'DiTrangNhatky');
    } catch (err) {
        tbody.innerHTML = `<tr><td colspan="6" style="text-align:center;color:red">Lỗi tải dữ liệu: ${EscapeHtml(err.message)}</td></tr>`;
    }
}
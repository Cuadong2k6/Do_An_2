// Dashboard - tải thống kê từ API
document.addEventListener('DOMContentLoaded', async () => {
    try {
        const res = await GoiAPI('/report/dashboard-stats');
        if (!res || !res.success || !res.data) {
            console.error('Không tải được dashboard:', res?.message || 'Phản hồi không hợp lệ.');
            return;
        }

        const d = res.data;

        // Stat cards
        const elSach = document.getElementById('statTongSach');
        const elDocGia = document.getElementById('statDocGia');
        const elQuaHan = document.getElementById('statQuaHan');
        if (elSach) elSach.textContent = d.tong_sach.toLocaleString('vi-VN');
        if (elDocGia) elDocGia.textContent = d.doc_gia_dang_ky.toLocaleString('vi-VN');
        if (elQuaHan) elQuaHan.textContent = d.qua_han_tra.toLocaleString('vi-VN');

        // Bảng giao dịch
        const tbody = document.getElementById('tblGiaoDichGanDay');
        if (!tbody || !d.giao_dich_gan_day) return;
        tbody.innerHTML = '';

        d.giao_dich_gan_day.forEach(g => {
            const maGD = EscapeHtml('#LN-' + g.ma_giao_dich.substring(0, 3).toUpperCase());
            const docGia = EscapeHtml(g.doc_gia);
            const ngayMuon = new Date(g.ngay_muon).toLocaleDateString('vi-VN');
            const trangThai = EscapeHtml(g.trang_thai);

            let badgeClass = 'badge-info';
            if (g.trang_thai === 'Quá hạn') badgeClass = 'badge-danger';
            else if (g.trang_thai === 'Đã trả') badgeClass = 'badge-success';

            const tr = document.createElement('tr');
            tr.innerHTML = '<td>' + maGD + '</td>' +
                           '<td>' + docGia + '</td>' +
                           '<td>' + ngayMuon + '</td>' +
                           '<td><span class="badge ' + badgeClass + '">' + trangThai + '</span></td>';
            tbody.appendChild(tr);
        });
    } catch (e) {
        console.error('Lỗi tải dashboard:', e);
    }
});
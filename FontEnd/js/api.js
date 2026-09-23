// ==========================================================================
// Base API Configuration and Helper functions
// ==========================================================================

const API_BASE_URL = 'http://localhost:5095/api'; // Hoặc https://localhost:7126/api tùy thuộc Backend

/**
 * Hàm gọi API chung (Wrapper cho fetch)
 * Tự động đính kèm Token và xử lý lỗi cơ bản
 */
async function fetchAPI(endpoint, method = 'GET', body = null) {
    const url = `${API_BASE_URL}${endpoint}`;
    
    // Cấu hình headers
    const headers = {
        'Content-Type': 'application/json',
    };

    // Lấy token từ localStorage (nếu có)
    const token = localStorage.getItem('token');
    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }

    // Nếu hệ thống dùng API Key, thêm vào header
    // headers['X-API-KEY'] = 'YOUR_API_KEY_HERE';

    const options = {
        method,
        headers,
    };

    if (body) {
        options.body = JSON.stringify(body);
    }

    try {
        const response = await fetch(url, options);
        
        // Xử lý Unauthorized
        if (response.status === 401) {
            localStorage.removeItem('token');
            localStorage.removeItem('user');
            window.location.href = '/index.html'; // Chuyển về trang login
            throw new Error("Phiên đăng nhập hết hạn.");
        }

        const data = await response.json();
        
        if (!response.ok) {
            throw new Error(data.message || `Lỗi API: ${response.status}`);
        }
        
        return data;
    } catch (error) {
        console.error('API Error:', error);
        throw error;
    }
}

// Export functions (nếu dùng ES6 module) hoặc để global (nếu add script trực tiếp)
window.api = {
    fetchAPI,
    get: (endpoint) => fetchAPI(endpoint, 'GET'),
    post: (endpoint, data) => fetchAPI(endpoint, 'POST', data),
    put: (endpoint, data) => fetchAPI(endpoint, 'PUT', data),
    delete: (endpoint) => fetchAPI(endpoint, 'DELETE')
};

// ==========================================================================
// Phân trang dùng chung cho các bảng Sách / Độc Giả / Phiếu mượn (10 hàng/trang)
//   tenHam: tên function toàn cục xử lý khi bấm nút trang, VD: 'diTrangSach'
// ==========================================================================
function renderphantrang(oBoxId, oInfoId, trang, tongHang, hangMoiTrang, tenHam) {
    const box = document.getElementById(oBoxId);
    const info = document.getElementById(oInfoId);
    const tongSoTrang = Math.max(1, Math.ceil((tongHang || 0) / hangMoiTrang));

    if (info) {
        if (tongHang > 0) {
            const dau = (trang - 1) * hangMoiTrang + 1;
            const cuoi = Math.min(trang * hangMoiTrang, tongHang);
            info.textContent = `Hiển thị ${dau} - ${cuoi} của ${tongHang} kết quả`;
        } else {
            info.textContent = 'Không có kết quả';
        }
    }
    if (!box) return;
    if (tongSoTrang <= 1) { box.innerHTML = ''; return; }

    const styleNut = 'padding:5px 10px;font-size:0.85rem';
    // khaDung = true → nút bấm được (màu xám); false → trang hiện tại (màu đậm, disabled)
    const nut = (nhan, tr, khaDung) =>
        `<button class="btn ${khaDung ? 'btn-secondary' : 'btn-primary'}" style="${styleNut}"` +
        (khaDung ? ` onclick="${tenHam}(${tr})"` : ' disabled') + `>${nhan}</button>`;

    // Hiển thị tối đa 5 nút số trang quanh trang hiện tại
    const dauHien = Math.max(1, Math.min(trang - 2, tongSoTrang - 4));
    const cuoiHien = Math.min(tongSoTrang, dauHien + 4);

    let html = nut('Trước', trang - 1, trang > 1);
    for (let i = dauHien; i <= cuoiHien; i++) html += nut(i, i, i !== trang);
    html += nut('Tiếp', trang + 1, trang < tongSoTrang);
    box.innerHTML = html;
}

import 'dart:convert';
import 'package:http/http.dart' as http;
import 'env.dart';
import 'models.dart';

class Api {
  final _client = http.Client();

  Uri _u(String path, [Map<String,String>? q]) =>
      Uri.parse('${Env.baseUrl}$path').replace(queryParameters: q);

  Future<List<Customer>> getCustomers() async {
    final r = await _client.get(_u('/api/customers'));
    if (r.statusCode != 200) throw Exception(r.body);
    final List a = jsonDecode(r.body);
    return a.map((e) => Customer.fromJson(e)).toList();
  }

  Future<Customer> createCustomer(String name, String phone, String? addr) async {
    final r = await _client.post(
      _u('/api/customers'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'fullName': name,
        'phone': phone,
        'address': addr,
      }),
    );
    if (r.statusCode != 201) throw Exception(r.body);
    return Customer.fromJson(jsonDecode(r.body));
  }

  Future<PickupRequest> createPickup({
    required int customerId,
    required String scrapType,
    required double quantityKg,
    required DateTime pickupTime,
    required double lat,
    required double lng,
    String? note,
  }) async {
    final r = await _client.post(
      _u('/api/pickups'),
      headers: {'Content-Type':'application/json'},
      body: jsonEncode({
        'customerId': customerId,
        'scrapType': scrapType,
        'quantityKg': quantityKg,
        'pickupTime': pickupTime.toIso8601String(),
        'lat': lat,
        'lng': lng,
        'note': note
      }),
    );
    if (r.statusCode != 201) throw Exception(r.body);
    return PickupRequest.fromJson(jsonDecode(r.body));
  }

  Future<List<PickupRequest>> getPickups({int? status, int? collectorId}) async {
    final q = <String,String>{};
    if (status != null) q['status'] = '$status';
    if (collectorId != null) q['collectorId'] = '$collectorId';

    final r = await _client.get(_u('/api/pickups', q.isEmpty ? null : q));
    if (r.statusCode != 200) throw Exception(r.body);

    final List a = jsonDecode(r.body);
    return a.map((e) => PickupRequest.fromJson(e)).toList();
  }

  Future<void> acceptPickup(int id, int collectorId) async {
    final r = await _client.post(
      _u('/api/pickups/$id/accept', {'collectorId': '$collectorId'}),
    );
    if (r.statusCode != 200) throw Exception(r.body);
  }

  Future<void> setStatus(int id, int status) async {
    final r = await _client.post(
      _u('/api/pickups/$id/status', {'status': '$status'}),
    );
    if (r.statusCode != 200) throw Exception(r.body);
  }

  Future<List<Collector>> getCollectors() async {
    final r = await _client.get(_u('/api/collectors'));
    if (r.statusCode != 200) throw Exception(r.body);
    final List a = jsonDecode(r.body);
    return a.map((e) => Collector.fromJson(e)).toList();
  }
}

// ===== Customers CRUD =====
extension ApiCustomers on Api {
  Future<Customer> getCustomer(int id) async {
    final r = await _client.get(_u('/api/customers/$id'));
    if (r.statusCode != 200) throw Exception(r.body);
    return Customer.fromJson(jsonDecode(r.body));
  }

  Future<Customer> updateCustomer(int id, Map<String,dynamic> body) async {
    final r = await _client.put(
      _u('/api/customers/$id'),
      headers:{'Content-Type':'application/json'},
      body: jsonEncode(body),
    );
    if (r.statusCode != 200) throw Exception(r.body);
    return Customer.fromJson(jsonDecode(r.body));
  }

  Future<void> deleteCustomer(int id) async {
    final r = await _client.delete(_u('/api/customers/$id'));
    if (r.statusCode != 204) throw Exception(r.body);
  }

  Future<void> updateCustomerLocation(int id, double lat, double lng) async {
    final r = await _client.post(
      _u('/api/customers/$id/location', {
        'lat':'$lat',
        'lng':'$lng'
      }),
    );
    if (r.statusCode != 200) throw Exception(r.body);
  }
}

// ===== Companies / Collectors CRUD =====
extension ApiCompanies on Api {

  // NEW: lấy danh sách công ty kèm danh sách collector của từng công ty
  //
  // Backend /api/companies trả về kiểu:
  // [
  //   {
  //     "id":1,
  //     "name":"Cty A",
  //     "contactPhone":"0909...",
  //     "address":"123 abc",
  //     "collectors":[
  //        {"id":10,"fullName":"Tèo","phone":"09..","companyId":1},
  //        ...
  //     ]
  //   },
  //   ...
  // ]
  //
  // Ta giữ nguyên dạng Map<String,dynamic> để ManagementScreen dùng thẳng.
  Future<List<Map<String, dynamic>>> getCompanies() async { // <-- NEW
    final r = await _client.get(_u('/api/companies'));
    if (r.statusCode != 200) throw Exception(r.body);

    final List raw = jsonDecode(r.body);
    return raw.cast<Map<String, dynamic>>();
  }

  Future<void> createCompany(String name, String phone, String? addr) async {
    final r = await _client.post(
      _u('/api/companies'),
      headers:{'Content-Type':'application/json'},
      body: jsonEncode({
        'name':name,
        'contactPhone':phone,
        'address':addr
      }),
    );
    if (r.statusCode != 201) throw Exception(r.body);
  }

  Future<void> updateCompany(int id, String name, String phone, String? addr) async {
    final r = await _client.put(
      _u('/api/companies/$id'),
      headers:{'Content-Type':'application/json'},
      body: jsonEncode({
        'id':id,
        'name':name,
        'contactPhone':phone,
        'address':addr
      }),
    );
    if (r.statusCode != 200) throw Exception(r.body);
  }

  Future<void> deleteCompany(int id) async {
    final r = await _client.delete(_u('/api/companies/$id'));
    if (r.statusCode != 204) throw Exception(r.body);
  }

  Future<void> createCollector(int companyId, String fullName, String phone) async {
    final r = await _client.post(
      _u('/api/collectors'),
      headers:{'Content-Type':'application/json'},
      body: jsonEncode({
        'companyId':companyId,
        'fullName':fullName,
        'phone':phone
      }),
    );
    if (r.statusCode != 201) throw Exception(r.body);
  }

  Future<void> updateCollector(int id, int companyId, String fullName, String phone) async {
    final r = await _client.put(
      _u('/api/collectors/$id'),
      headers:{'Content-Type':'application/json'},
      body: jsonEncode({
        'id':id,
        'companyId':companyId,
        'fullName':fullName,
        'phone':phone
      }),
    );
    if (r.statusCode != 200) throw Exception(r.body);
  }

  Future<void> deleteCollector(int id) async {
    final r = await _client.delete(_u('/api/collectors/$id'));
    if (r.statusCode != 204) throw Exception(r.body);
  }
}

// ===== Listings (search + create) =====
class Listing {
  final int id;
  final String title;
  final String description;
  final double? lat;
  final double? lng;
  final double pricePerKg;
  final String createdAt;

  Listing({
    required this.id,
    required this.title,
    required this.description,
    this.lat,
    this.lng,
    required this.pricePerKg,
    required this.createdAt
  });

  factory Listing.fromJson(Map<String,dynamic> j)=> Listing(
    id: j['id'],
    title: j['title'],
    description: j['description'],
    lat: j['lat'] == null ? null : (j['lat'] as num).toDouble(),
    lng: j['lng'] == null ? null : (j['lng'] as num).toDouble(),
    pricePerKg: (j['pricePerKg'] as num).toDouble(),
    createdAt: j['createdAt'],
  );
}

extension ApiListings on Api {
  Future<void> createListing(
    String title,
    String desc,
    double pricePerKg,
    {double? lat, double? lng}
  ) async {
    final r = await _client.post(
      _u('/api/listings'),
      headers:{'Content-Type':'application/json'},
      body: jsonEncode({
        'title':title,
        'description':desc,
        'pricePerKg':pricePerKg,
        'lat':lat,
        'lng':lng,
      }),
    );
    if (r.statusCode != 201) throw Exception(r.body);
  }

  Future<List<Listing>> searchListings({
    String? q,
    double? lat,
    double? lng,
    double? radiusKm,
    int? top
  }) async {
    final qp = <String,String>{};

    if (q != null && q.isNotEmpty) qp['q'] = q;
    if (lat != null && lng != null && radiusKm != null) {
      qp['lat'] = '$lat';
      qp['lng'] = '$lng';
      qp['radiusKm'] = '$radiusKm';
    }
    if (top != null) qp['top'] = '$top';

    final r = await _client.get(_u('/api/listings/search', qp.isEmpty ? null : qp));
    if (r.statusCode != 200) throw Exception(r.body);

    final List a = jsonDecode(r.body);
    return a.map((e)=> Listing.fromJson(e)).toList();
  }
}

// ===== Dispatch nearest =====
extension ApiDispatch on Api {
  Future<void> dispatchNearest(
    int pickupId, {
    required double jobLat,
    required double jobLng,
    double radiusKm = 10,
    int? companyId,
  }) async {
    final q = {
      'jobLat':'$jobLat',
      'jobLng':'$jobLng',
      'radiusKm':'$radiusKm',
      if (companyId != null) 'companyId':'$companyId'
    };
    final r = await _client.post(_u('/api/pickups/$pickupId/dispatch-nearest', q));
    if (r.statusCode != 200) throw Exception(r.body);
  }
}

// ===== Collector helper (vị trí + việc của tôi) =====
extension ApiCollectorExt on Api {
  // Collector app gửi tọa độ GPS hiện tại để backend lưu CurrentLat/CurrentLng
  Future<void> updateCollectorLocation(int id, double lat, double lng) async {
    final r = await _client.post(
      _u('/api/collectors/$id/location', {
        'lat': '$lat',
        'lng': '$lng',
      }),
    );
    if (r.statusCode != 200) throw Exception(r.body);
  }

  // Lấy danh sách pickup của riêng collector đó
  Future<List<PickupRequest>> getMyPickups(int collectorId, {int? status}) async {
    final q = <String,String>{};
    if (status != null) q['status'] = '$status';

    final r = await _client.get(
      _u('/api/collectors/$collectorId/pickups', q.isEmpty ? null : q),
    );
    if (r.statusCode != 200) throw Exception(r.body);

    final List a = jsonDecode(r.body);
    return a.map((e) => PickupRequest.fromJson(e)).toList();
  }
}

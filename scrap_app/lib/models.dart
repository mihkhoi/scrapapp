class Customer {
  final int id;
  final String fullName;
  final String phone;
  final String? address;

  Customer({required this.id, required this.fullName, required this.phone, this.address});

  factory Customer.fromJson(Map<String, dynamic> j) =>
      Customer(id: j['id'], fullName: j['fullName'], phone: j['phone'], address: j['address']);
}

enum PickupStatus { pending, accepted, inProgress, completed, cancelled }

class PickupRequest {
  final int id;
  final Customer? customer;
  final String scrapType;
  final double quantityKg;
  final DateTime pickupTime;
  final double lat;
  final double lng;
  final String? note;
  final int status; // 0..4

  PickupRequest({
    required this.id, required this.customer, required this.scrapType, required this.quantityKg,
    required this.pickupTime, required this.lat, required this.lng, this.note, required this.status
  });

  factory PickupRequest.fromJson(Map<String, dynamic> j) => PickupRequest(
    id: j['id'],
    customer: j['customer'] == null ? null : Customer.fromJson(j['customer']),
    scrapType: j['scrapType'],
    quantityKg: (j['quantityKg'] as num).toDouble(),
    pickupTime: DateTime.parse(j['pickupTime']),
    lat: (j['lat'] as num).toDouble(),
    lng: (j['lng'] as num).toDouble(),
    note: j['note'],
    status: j['status'],
  );
}

class Collector {
  final int id; final String fullName;
  Collector(this.id, this.fullName);
  factory Collector.fromJson(Map<String,dynamic> j) => Collector(j['id'], j['fullName']);
}

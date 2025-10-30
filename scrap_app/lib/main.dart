import 'package:flutter/material.dart';
import 'ui/app_theme.dart';
import 'screens/customer_booking_screen.dart';
import 'screens/collector_screen.dart';
import 'screens/management_screen.dart';
import 'screens/listings_screen.dart';

void main() {
  runApp(const ScrapApp());
}

class ScrapApp extends StatelessWidget {
  const ScrapApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Scrap Manager',
      theme: AppTheme.light(),
      home: const Home(),
    );
  }
}

// model nhỏ cho ô menu
class _HomeItem {
  final IconData icon;
  final String label;
  final Widget screen;
  const _HomeItem(this.icon, this.label, this.screen);
}

class Home extends StatelessWidget {
  const Home({super.key});

  @override
  Widget build(BuildContext context) {
    // danh sách nút trên màn hình chính
    final List<_HomeItem> items = [
      _HomeItem(Icons.calendar_today_rounded, 'Khách đặt lịch', const CustomerBookingScreen()),
      _HomeItem(Icons.recycling_rounded, 'Bên thu gom', const CollectorScreen()),
      _HomeItem(Icons.admin_panel_settings, 'Quản lý', const ManagementScreen()),
      _HomeItem(Icons.store_mall_directory, 'Nguồn cung', const ListingsScreen()),
    ];

    return Scaffold(
      appBar: AppBar(
        title: const Text('Scrap Manager'),
      ),
      body: GridView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: items.length,
        gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
          crossAxisCount: 2,
          childAspectRatio: 1.2,
          crossAxisSpacing: 16,
          mainAxisSpacing: 16,
        ),
        itemBuilder: (BuildContext ctx, int i) {
          final item = items[i];
          return InkWell(
            borderRadius: BorderRadius.circular(20),
            onTap: () {
              Navigator.push(
                ctx,
                MaterialPageRoute(
                  builder: (context) => item.screen,
                ),
              );
            },
            child: Card(
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(20),
              ),
              elevation: 2,
              child: Center(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(item.icon, size: 40),
                    const SizedBox(height: 12),
                    Text(
                      item.label,
                      textAlign: TextAlign.center,
                      style: const TextStyle(
                        fontWeight: FontWeight.w600,
                        fontSize: 15,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          );
        },
      ),
    );
  }
}

-- 初始化测试数据脚本
-- 在数据库创建后运行此脚本以添加测试数据

-- 添加医生数据
INSERT INTO Doctors (Name, Title, Department, Specialization, Introduction, IsActive, CreatedAt)
VALUES
('张伟', '主任医师', '内科', '心血管疾病、高血压、冠心病', '从业20年，擅长心血管疾病的诊断和治疗', 1, datetime('now')),
('李娜', '副主任医师', '儿科', '儿童呼吸系统疾病、儿童哮喘', '儿科专家，对儿童呼吸道疾病有丰富经验', 1, datetime('now')),
('王强', '主治医师', '外科', '普通外科手术、微创手术', '擅长各类普外科手术，腹腔镜手术经验丰富', 1, datetime('now')),
('赵敏', '主任医师', '妇产科', '妇科肿瘤、不孕不育', '妇产科专家，擅长妇科疾病诊治', 1, datetime('now')),
('刘洋', '副主任医师', '骨科', '骨折、关节疾病、脊柱疾病', '骨科专业，擅长各类骨折和关节疾病治疗', 1, datetime('now'));

-- 添加未来一周的排班数据
-- 张伟医生的排班
INSERT INTO Schedules (DoctorId, Date, TimeSlot, TotalSlots, AvailableSlots, CreatedAt)
VALUES
(1, date('now', '+1 day'), '上午', 20, 20, datetime('now')),
(1, date('now', '+1 day'), '下午', 15, 15, datetime('now')),
(1, date('now', '+2 day'), '上午', 20, 20, datetime('now')),
(1, date('now', '+3 day'), '上午', 20, 20, datetime('now')),
(1, date('now', '+3 day'), '下午', 15, 15, datetime('now'));

-- 李娜医生的排班
INSERT INTO Schedules (DoctorId, Date, TimeSlot, TotalSlots, AvailableSlots, CreatedAt)
VALUES
(2, date('now', '+1 day'), '上午', 15, 15, datetime('now')),
(2, date('now', '+1 day'), '下午', 15, 15, datetime('now')),
(2, date('now', '+2 day'), '上午', 15, 15, datetime('now')),
(2, date('now', '+4 day'), '上午', 15, 15, datetime('now'));

-- 王强医生的排班
INSERT INTO Schedules (DoctorId, Date, TimeSlot, TotalSlots, AvailableSlots, CreatedAt)
VALUES
(3, date('now', '+1 day'), '上午', 10, 10, datetime('now')),
(3, date('now', '+2 day'), '上午', 10, 10, datetime('now')),
(3, date('now', '+2 day'), '下午', 8, 8, datetime('now')),
(3, date('now', '+5 day'), '上午', 10, 10, datetime('now'));

-- 赵敏医生的排班
INSERT INTO Schedules (DoctorId, Date, TimeSlot, TotalSlots, AvailableSlots, CreatedAt)
VALUES
(4, date('now', '+1 day'), '上午', 12, 12, datetime('now')),
(4, date('now', '+1 day'), '下午', 12, 12, datetime('now')),
(4, date('now', '+3 day'), '上午', 12, 12, datetime('now')),
(4, date('now', '+4 day'), '下午', 12, 12, datetime('now'));

-- 刘洋医生的排班
INSERT INTO Schedules (DoctorId, Date, TimeSlot, TotalSlots, AvailableSlots, CreatedAt)
VALUES
(5, date('now', '+1 day'), '上午', 15, 15, datetime('now')),
(5, date('now', '+2 day'), '上午', 15, 15, datetime('now')),
(5, date('now', '+2 day'), '下午', 10, 10, datetime('now')),
(5, date('now', '+4 day'), '上午', 15, 15, datetime('now'));

-- 添加常用药品数据
INSERT INTO Medicines (Name, Specification, Unit, Price, Stock, Category, Description, IsActive, CreatedAt)
VALUES
-- 抗生素类
('阿莫西林胶囊', '0.25g*24粒', '盒', 15.50, 1000, '抗生素', '用于敏感菌所致的各种感染', 1, datetime('now')),
('头孢克肟分散片', '50mg*12片', '盒', 28.00, 800, '抗生素', '用于细菌感染性疾病', 1, datetime('now')),
('阿奇霉素片', '0.25g*6片', '盒', 32.00, 600, '抗生素', '用于敏感细菌引起的感染', 1, datetime('now')),

-- 解热镇痛类
('布洛芬片', '0.2g*20片', '盒', 8.00, 800, '解热镇痛', '用于缓解轻至中度疼痛及发热', 1, datetime('now')),
('对乙酰氨基酚片', '0.5g*16片', '盒', 6.50, 1200, '解热镇痛', '用于发热、头痛等症状', 1, datetime('now')),
('双氯芬酸钠缓释片', '75mg*10片', '盒', 18.00, 500, '解热镇痛', '用于关节炎、肌肉痛等', 1, datetime('now')),

-- 消化系统用药
('奥美拉唑肠溶胶囊', '20mg*14粒', '盒', 22.00, 600, '消化系统', '用于胃溃疡、胃食管反流病', 1, datetime('now')),
('多潘立酮片', '10mg*30片', '盒', 16.50, 400, '消化系统', '用于消化不良、恶心呕吐', 1, datetime('now')),
('蒙脱石散', '3g*10袋', '盒', 24.00, 500, '消化系统', '用于急慢性腹泻', 1, datetime('now')),

-- 心血管系统用药
('硝苯地平缓释片', '20mg*30片', '盒', 26.00, 400, '心血管', '用于高血压、心绞痛', 1, datetime('now')),
('阿托伐他汀钙片', '20mg*7片', '盒', 58.00, 300, '心血管', '用于降低胆固醇', 1, datetime('now')),
('阿司匹林肠溶片', '100mg*30片', '盒', 12.00, 800, '心血管', '用于预防血栓形成', 1, datetime('now')),

-- 呼吸系统用药
('氨溴索片', '30mg*20片', '盒', 18.50, 600, '呼吸系统', '用于痰液粘稠不易咳出', 1, datetime('now')),
('孟鲁司特钠片', '10mg*7片', '盒', 68.00, 300, '呼吸系统', '用于哮喘的预防和治疗', 1, datetime('now')),
('复方甘草片', '50片', '瓶', 5.00, 1000, '呼吸系统', '用于镇咳祛痰', 1, datetime('now')),

-- 维生素和营养补充剂
('维生素C片', '100mg*100片', '瓶', 12.00, 500, '维生素', '补充维生素C', 1, datetime('now')),
('复合维生素B片', '100片', '瓶', 15.00, 600, '维生素', '补充B族维生素', 1, datetime('now')),
('钙尔奇D片', '600mg*60片', '瓶', 78.00, 400, '矿物质', '补充钙和维生素D', 1, datetime('now')),

-- 中成药
('感冒灵颗粒', '10g*9袋', '盒', 16.00, 800, '中成药', '用于感冒引起的头痛发热', 1, datetime('now')),
('板蓝根颗粒', '10g*20袋', '盒', 18.00, 700, '中成药', '用于病毒性感冒', 1, datetime('now')),
('藿香正气水', '10ml*10支', '盒', 12.50, 600, '中成药', '用于暑湿感冒、肠胃不适', 1, datetime('now'));

-- 添加一个测试患者
INSERT INTO Patients (Name, Gender, DateOfBirth, IdCard, Phone, Address, Allergies, MedicalHistory, FamilyHistory, CreatedAt, UpdatedAt)
VALUES
('张三', '男', date('1985-05-15'), '110101198505151234', '13800138000', '北京市朝阳区某某街道', '青霉素过敏', '高血压病史5年', '父亲有糖尿病', datetime('now'), datetime('now'));

SELECT '数据初始化完成！' AS Message;
SELECT '已添加 ' || COUNT(*) || ' 位医生' AS DoctorCount FROM Doctors;
SELECT '已添加 ' || COUNT(*) || ' 条排班记录' AS ScheduleCount FROM Schedules;
SELECT '已添加 ' || COUNT(*) || ' 种药品' AS MedicineCount FROM Medicines;
SELECT '已添加 ' || COUNT(*) || ' 位测试患者' AS PatientCount FROM Patients;

#!/usr/bin/env python3
import sqlite3
import sys

def init_database():
    # 连接数据库
    conn = sqlite3.connect('backend/medical.db')
    cursor = conn.cursor()

    # 读取SQL脚本
    with open('backend/init-data.sql', 'r', encoding='utf-8') as f:
        sql_script = f.read()

    # 分割并执行SQL语句
    statements = sql_script.split(';')

    for statement in statements:
        statement = statement.strip()
        if statement and not statement.startswith('--'):
            try:
                cursor.execute(statement)
            except sqlite3.Error as e:
                print(f"执行SQL出错: {e}")
                print(f"语句: {statement[:100]}...")

    # 提交更改
    conn.commit()

    # 验证数据
    cursor.execute("SELECT COUNT(*) FROM Doctors")
    doctor_count = cursor.fetchone()[0]

    cursor.execute("SELECT COUNT(*) FROM Schedules")
    schedule_count = cursor.fetchone()[0]

    cursor.execute("SELECT COUNT(*) FROM Medicines")
    medicine_count = cursor.fetchone()[0]

    cursor.execute("SELECT COUNT(*) FROM Patients")
    patient_count = cursor.fetchone()[0]

    print("数据库初始化完成！")
    print(f"已添加 {doctor_count} 位医生")
    print(f"已添加 {schedule_count} 条排班记录")
    print(f"已添加 {medicine_count} 种药品")
    print(f"已添加 {patient_count} 位测试患者")

    # 关闭连接
    conn.close()

if __name__ == "__main__":
    try:
        init_database()
    except Exception as e:
        print(f"初始化失败: {e}")
        sys.exit(1)

# السنتر · Center

نظام إدارة سنتر تعليمي: الطلاب والمجموعات، الحضور، الماتريال والواجبات، الاختبارات، التحصيل الشهري، الرسائل، وبوابة للطالب.

A tutoring-center system for students, groups, attendance, learning content, exams, monthly billing, messages, and a student portal.

الأسماء والأرقام في اللقطات بيانات عرض، مش بيانات حقيقية.

Names and numbers in the screenshots are sample data, not real records.

## الترجمة · Localization

الواجهة مترجمة بالكامل بين **العربية** و**الإنجليزية**. اللغة الافتراضية عربي (مصر)، والاتجاه بيتظبط لوحده: RTL بالعربي و LTR بالإنجليزي. التبديل من زر اللغة في الشريط العلوي، والنصوص في ملفات `SharedResource.resx` و `SharedResource.en.resx`.

The whole interface is localized in **Arabic** and **English**. Arabic (Egypt) is the default, and layout direction follows the language: RTL for Arabic, LTR for English. The switch is in the top bar. Strings live in `SharedResource.resx` and `SharedResource.en.resx`.

لوحة التحكم بالإنجليزي.

Dashboard in English.

![Dashboard in English](docs/screenshots/dashboard-en.png)

قائمة الطلاب بالإنجليزي: نفس الشاشة، عناوين وأزرار مترجمة.

The student list in English: same screen, with translated labels and buttons.

![Students in English](docs/screenshots/students-en.png)

الاختبارات الأونلاين بالإنجليزي.

Online exams in English.

![Online exams in English](docs/screenshots/exams-en.png)

## الدخول ولوحة التحكم · Sign-in and dashboard

تسجيل الدخول. / Sign in.

![Login](docs/screenshots/login.png)

ملخص اليوم: الطلاب، المجموعات، الحضور، وحسابات الشهر. / Today’s summary: students, groups, attendance, and this month’s accounts.

![Dashboard](docs/screenshots/dashboard.png)

## الطلاب · Students

القائمة: بحث، كود، كارنيه QR، وحساب دخول. / The list: search, code, QR card, and a portal login.

![Students](docs/screenshots/students.png)

إضافة طالب. الكود يتولد من المجموعة. / Add a student. The code is generated from the group.

![New student](docs/screenshots/student-new.png)

تعديل طالب: البيانات، المجموعة الحالية، والنقل. / Edit a student: details, current group, and transfer.

![Edit student](docs/screenshots/student-edit.png)

الحذف بيطلب تأكيد قبل ما يتم. / Delete asks for confirmation first.

![Delete student](docs/screenshots/student-delete.png)

## المجموعات · Groups

المواعيد الأسبوعية، المدرس، المادة، والسعر. / Weekly schedule, teacher, subject, and fee.

![Groups](docs/screenshots/groups.png)

مجموعة جديدة. / New group.

![New group](docs/screenshots/group-new.png)

تعديل المجموعة وجدولها. / Edit the group and its weekly schedule.

![Edit group](docs/screenshots/group-edit.png)

تأكيد حذف المجموعة. / Confirm deleting a group.

![Delete group](docs/screenshots/group-delete.png)

## الحضور · Attendance

حصص الشهر، وتوليدها من جدول المجموعة. / The month’s sessions, generated from the group schedule.

![Sessions](docs/screenshots/attendance.png)

التحضير: حاضر، غائب، بعذر، متأخر، مع تحضير بالـ QR. / Take attendance: present, absent, excused, late, including QR check-in.

![Take attendance](docs/screenshots/attendance-take.png)

## التحصيل · Billing

فاتورة الشهر: المستحق، المدفوع، والمتبقي، مع تسجيل الدفع والخصم والسماح. / The month’s invoice: due, paid, and remaining, plus payment, discount, and grace.

![Invoices](docs/screenshots/invoices.png)

## المحتوى · Content

الماتريال المنشور للمجموعات. / Materials published to groups.

![Materials](docs/screenshots/materials.png)

تعديل ماتريال: العنوان، المجموعات، ورفع فيديو أو PDF أو صور. / Edit a material: title, groups, and video, PDF, or image upload.

![Edit material](docs/screenshots/material-edit.png)

الواجبات. / Homework.

![Homework](docs/screenshots/homework.png)

تعديل واجب وملفاته. / Edit a homework item and its files.

![Edit homework](docs/screenshots/homework-edit.png)

## الاختبارات · Exams

الاختبارات الأونلاين: نافذة الفتح والإغلاق، النشر، والدرجات. / Online exams: open and close window, publish, and grades.

![Online exams](docs/screenshots/exams.png)

شاشة الاختبار: العنوان، مواعيد الفتح والإغلاق وظهور النتيجة، والمجموعات، والأسئلة. الاختبار المنشور بيتقفل تعديله بعد ما حد يبدأ. / The exam itself: title, open, close, and results times, groups, and questions. A published exam locks once an attempt has started.

![Edit exam](docs/screenshots/exam-edit.png)

درجات المحاولات، وإعادة فتح محاولة لطالب. / Attempt grades, and reopening an attempt for a student.

![Exam grades](docs/screenshots/exam-grades.png)

رصد درجات الاختبار الورقي. / Offline exam scores.

![Offline exams](docs/screenshots/offline-exams.png)

## الرسائل · Messages

أسئلة الطلاب للمدرس، مع عدد غير المقروء. / Student questions to the teacher, with an unread count.

![Messages](docs/screenshots/messages.png)

المحادثة: رد المدرس ومسح الشات. / The thread: the teacher’s reply, and clearing the chat.

![Message thread](docs/screenshots/message-thread.png)

## التقارير والصلاحيات · Reports and access

تقارير الحضور والدرجات لفترة ومجموعة. / Attendance and grade reports for a period and a group.

![Reports](docs/screenshots/reports.png)

المستخدمون وأدوارهم. / Users and their roles.

![Users](docs/screenshots/users.png)

مصفوفة الصلاحيات: عرض، إضافة، تعديل، وحذف لكل شاشة. / The permission matrix: view, add, edit, and delete per screen.

![Role permissions](docs/screenshots/role-edit.png)

## ماذا يغطّي النظام · What it covers

**التشغيل اليومي · Day to day**
- طلاب بكود ثابت وكارنيه QR، ونقل بين المجموعات. Students with a stable code and QR card, and transfers between groups.
- مجموعات بمدرس ومادة ومرحلة وسعر شهري وجدول أسبوعي. Groups with a teacher, subject, grade, monthly fee, and weekly schedule.
- توليد حصص الشهر والتحضير. Generate the month’s sessions and take attendance.

**المحتوى والاختبار · Content and exams**
- ماتريال: فيديو، PDF، وصور. واجبات PDF و Word. Materials: video, PDF, and images. Homework as PDF or Word.
- اختبار أونلاين باختيارات متعددة، وتسليم تلقائي، وإعادة فتح المحاولة. Online multiple-choice exams, auto-submit, and reopening an attempt.
- درجات ورقي بجانب الأونلاين، وتقارير قابلة للطباعة. Offline scores beside online exams, and printable reports.

**الحسابات · Accounts**
- فاتورة شهرية، خصم نسبة أو مبلغ، ودفعات كاش أو محفظة أو تحويل. A monthly invoice, a percent or fixed discount, and cash, wallet, or transfer payments.
- سماح مؤقت بدخول الماتريال. A temporary grace period for material access.

**التواصل والصلاحيات · Messages and permissions**
- رسائل بين الطالب والمدرس مع إشعارات فورية (SignalR). Student–teacher messages with live notifications (SignalR).
- ثلاثة أدوار: مدير، مدرس، طالب. كل شاشة لها عرض وإضافة وتعديل وحذف. Three roles: admin, teacher, and student. Each screen has view, add, edit, and delete.
- بوابة الطالب: ماتريال، واجبات، اختبارات، حضور، وسؤال للمدرس. The student portal: materials, homework, exams, attendance, and a question to the teacher.
- جلسة واحدة نشطة للحساب. One active session per account.

| الدور · Role | ما يراه · What they see |
| --- | --- |
| المدير · Admin | كل الشاشات، بما فيها المستخدمين والصلاحيات. Every screen, including users and permissions. |
| المدرس · Teacher | مجموعاته، والمحتوى، والاختبارات، والرسائل، والتقارير. His groups, content, exams, messages, and reports. |
| الطالب · Student | البوابة فقط. The portal only. |

القائمة بتظهر الشاشات اللي للمستخدم صلاحية عرض عليها. The menu only lists screens the user is allowed to view.

## التقنيات · Stack

- ASP.NET Core 8 MVC
- Entity Framework Core 8 and SQL Server
- ASP.NET Core Identity
- SignalR for messages and notifications
- Bootstrap and Cairo, with RTL / LTR from the active culture

```
CenterApp.Web        الواجهة والمتحكمات · UI and controllers
CenterApp.Service    قواعد العمل والترجمة والـ Migrations · business rules, localization, migrations
CenterApp.Entity     الكيانات والصلاحيات · entities and permissions
CenterApp.Tests      اختبارات · tests
```

## التشغيل · Run

المطلوب: .NET 8 SDK، و SQL Server LocalDB (أو أي SQL Server بعد تعديل سلسلة الاتصال).

Requires the .NET 8 SDK and SQL Server LocalDB, or any SQL Server after you change the connection string.

الاتصال الافتراضي في `CenterApp.Web/appsettings.json`:

Default connection in `CenterApp.Web/appsettings.json`:

```
Server=(localdb)\MSSQLLocalDB;Database=CenterApp;Trusted_Connection=True;TrustServerCertificate=True
```

```bash
dotnet run --project CenterApp.Web
```

أول تشغيل يطبّق الـ Migrations وينشئ المدير من إعداد `Seed` لو مش موجود. غيّر بيانات `Seed` قبل أي استخدام خارج جهازك.

The first run applies migrations and creates the admin from `Seed` if that account does not exist. Change the `Seed` values before using this anywhere but your own machine.

ملفات الماتريال والواجبات تحت `CenterApp.Web/App_Data`. تحويل صفحات المستندات لصور يستخدم LibreOffice لو كان متسطبًا، والمسار في `Material:LibreOfficePath`.

Material and homework files are stored under `CenterApp.Web/App_Data`. Turning document pages into images uses LibreOffice when it is installed; the path is `Material:LibreOfficePath`.

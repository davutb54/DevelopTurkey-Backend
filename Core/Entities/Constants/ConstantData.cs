namespace Core.Entities.Constants;

public static class ConstantData
{
	public static List<City> Cities = new List<City>
	{
		new City(1, "Adana", "Adana"),
		new City(2, "Adıyaman", "Adıyaman"),
		new City(3, "Afyon", "Afyon"),
		new City(4, "Ağrı", "Ağrı"),
		new City(5, "Amasya", "Amasya"),
		new City(6, "Ankara", "Ankara"),
		new City(7, "Antalya", "Antalya"),
		new City(8, "Artvin", "Artvin"),
		new City(9, "Aydın", "Aydın"),
		new City(10, "Balıkesir", "Balıkesir"),
		new City(11, "Bilecik", "Bilecik"),
		new City(12, "Bingöl", "Bingöl"),
		new City(13, "Bitlis", "Bitlis"),
		new City(14, "Bolu", "Bolu"),
		new City(15, "Burdur", "Burdur"),
		new City(16, "Bursa", "Bursa"),
		new City(17, "Çanakkale", "Çanakkale"),
		new City(18, "Çankırı", "Çankırı"),
		new City(19, "Çorum", "Çorum"),
		new City(20, "Denizli", "Denizli"),
		new City(21, "Diyarbakır", "Diyarbakır"),
		new City(22, "Edirne", "Edirne"),
		new City(23, "Elazığ", "Elazığ"),
		new City(24, "Erzincan", "Erzincan"),
		new City(25, "Erzurum", "Erzurum"),
		new City(26, "Eskişehir", "Eskişehir"),
		new City(27, "Gaziantep", "Gaziantep"),
		new City(28, "Giresun", "Giresun"),
		new City(29, "Gümüşhane", "Gümüşhane"),
		new City(30, "Hakkari", "Hakkari"),
		new City(31, "Hatay", "Hatay"),
		new City(32, "Isparta", "Isparta"),
		new City(33, "Mersin ", "Mersin "),
		new City(34, "İstanbul", "İstanbul"),
		new City(35, "İzmir", "İzmir"),
		new City(36, "Kars", "Kars"),
		new City(37, "Kastamonu", "Kastamonu"),
		new City(38, "Kayseri", "Kayseri"),
		new City(39, "Kırklareli", "Kırklareli"),
		new City(40, "Kırşehir", "Kırşehir"),
		new City(41, "Kocaeli", "Kocaeli"),
		new City(42, "Konya", "Konya"),
		new City(43, "Kütahya", "Kütahya"),
		new City(44, "Malatya", "Malatya"),
		new City(45, "Manisa", "Manisa"),
		new City(46, "Kahramanmaraş", "Kahramanmaraş"),
		new City(47, "Mardin", "Mardin"),
		new City(48, "Muğla", "Muğla"),
		new City(49, "Muş", "Muş"),
		new City(50, "Nevşehir", "Nevşehir"),
		new City(51, "Niğde", "Niğde"),
		new City(52, "Ordu", "Ordu"),
		new City(53, "Rize", "Rize"),
		new City(54, "Sakarya", "Sakarya"),
		new City(55, "Samsun", "Samsun"),
		new City(56, "Siirt", "Siirt"),
		new City(57, "Sinop", "Sinop"),
		new City(58, "Sivas", "Sivas"),
		new City(59, "Tekirdağ", "Tekirdağ"),
		new City(60, "Tokat", "Tokat"),
		new City(61, "Trabzon", "Trabzon"),
		new City(62, "Tunceli", "Tunceli"),
		new City(63, "Şanlıurfa", "Şanlıurfa"),
		new City(64, "Uşak", "Uşak"),
		new City(65, "Van", "Van"),
		new City(66, "Yozgat", "Yozgat"),
		new City(67, "Zonguldak", "Zonguldak"),
		new City(68, "Aksaray", "Aksaray"),
		new City(69, "Bayburt", "Bayburt"),
		new City(70, "Karaman", "Karaman"),
		new City(71, "Kırıkkale", "Kırıkkale"),
		new City(72, "Batman", "Batman"),
		new City(73, "Şırnak", "Şırnak"),
		new City(74, "Bartın", "Bartın"),
		new City(75, "Ardahan", "Ardahan"),
		new City(76, "Iğdır", "Iğdır"),
		new City(77, "Yalova", "Yalova"),
		new City(78, "Karabük", "Karabük"),
		new City(79, "Kilis", "Kilis"),
		new City(80, "Osmaniye", "Osmaniye"),
		new City(81, "Düzce", "Düzce"),
	};

	public static List<Gender> Genders = new List<Gender>
	{
		new Gender(1, "Erkek", "Male"),
		new Gender(0, "Kadın", "Female"),
	};

	public static City GetCity(int cityCode)
	{
		return Cities.First(city => city.Value == cityCode);
	}

	public static Gender GetGender(int genderCode)
	{
		return Genders.First(gender => gender.Value == genderCode);
	}
}
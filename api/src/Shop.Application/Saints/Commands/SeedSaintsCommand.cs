using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;
using SynDock.Core.Common;

namespace Shop.Application.Saints.Commands;

public record SeedSaintsCommand() : IRequest<Result<int>>;

public class SeedSaintsCommandHandler : IRequestHandler<SeedSaintsCommand, Result<int>>
{
    private readonly IShopDbContext _db;

    public SeedSaintsCommandHandler(IShopDbContext db)
    {
        _db = db;
    }

    public async Task<Result<int>> Handle(SeedSaintsCommand request, CancellationToken cancellationToken)
    {
        var existingCount = await _db.Saints.CountAsync(cancellationToken);
        if (existingCount > 0)
            return Result<int>.Failure("이미 성인 데이터가 존재합니다.");

        var saints = GetSeedSaints();
        await _db.Saints.AddRangeAsync(saints, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(saints.Count);
    }

    private static List<Saint> GetSeedSaints()
    {
        return new List<Saint>
        {
            new() { KoreanName = "성모 마리아", LatinName = "Beata Maria Virgo", EnglishName = "Blessed Virgin Mary", Description = "예수 그리스도의 어머니이자 하느님의 어머니. 가톨릭 교회에서 가장 공경받는 성인.", FeastDay = new DateTime(2000, 8, 15), Patronage = "대한민국, 모든 인류의 어머니", CreatedBy = "system" },
            new() { KoreanName = "성 요셉", LatinName = "Sanctus Ioseph", EnglishName = "Saint Joseph", Description = "성모 마리아의 배우자이자 예수의 양부. 노동자의 수호성인.", FeastDay = new DateTime(2000, 3, 19), Patronage = "노동자, 가정, 임종자", CreatedBy = "system" },
            new() { KoreanName = "성 베드로", LatinName = "Sanctus Petrus", EnglishName = "Saint Peter", Description = "예수의 열두 사도 중 수제자. 초대 교황.", FeastDay = new DateTime(2000, 6, 29), Patronage = "교황, 어부, 자물쇠 제조업자", CreatedBy = "system" },
            new() { KoreanName = "성 바오로", LatinName = "Sanctus Paulus", EnglishName = "Saint Paul", Description = "이방인의 사도. 기독교 신학의 기초를 세운 위대한 사도.", FeastDay = new DateTime(2000, 6, 29), Patronage = "선교사, 신학자, 언론인", CreatedBy = "system" },
            new() { KoreanName = "성 프란치스코", LatinName = "Sanctus Franciscus Assisiensis", EnglishName = "Saint Francis of Assisi", Description = "프란치스코회 창설자. 청빈과 자연 사랑으로 유명.", FeastDay = new DateTime(2000, 10, 4), Patronage = "동물, 자연환경, 이탈리아", CreatedBy = "system" },
            new() { KoreanName = "성녀 데레사 (아빌라)", LatinName = "Sancta Teresia a Iesu", EnglishName = "Saint Teresa of Ávila", Description = "가르멜 수녀회 개혁자. 신비 신학의 대가이자 교회 학자.", FeastDay = new DateTime(2000, 10, 15), Patronage = "두통 환자, 스페인", CreatedBy = "system" },
            new() { KoreanName = "성녀 소화 데레사", LatinName = "Sancta Teresia a Iesu Infante", EnglishName = "Saint Thérèse of Lisieux", Description = "작은 꽃이라 불리는 가르멜 수녀. '작은 길'의 영성을 가르침.", FeastDay = new DateTime(2000, 10, 1), Patronage = "선교사, 꽃집, 프랑스", CreatedBy = "system" },
            new() { KoreanName = "성 안토니오 (파도바)", LatinName = "Sanctus Antonius Patavinus", EnglishName = "Saint Anthony of Padua", Description = "프란치스코회 사제. 뛰어난 설교가이자 교회 학자.", FeastDay = new DateTime(2000, 6, 13), Patronage = "분실물, 가난한 이, 여행자", CreatedBy = "system" },
            new() { KoreanName = "성 토마스 아퀴나스", LatinName = "Sanctus Thomas Aquinas", EnglishName = "Saint Thomas Aquinas", Description = "도미니코회 신학자. 스콜라 철학의 대가이자 교회 학자.", FeastDay = new DateTime(2000, 1, 28), Patronage = "학생, 대학, 철학자", CreatedBy = "system" },
            new() { KoreanName = "성 아우구스티노", LatinName = "Sanctus Augustinus", EnglishName = "Saint Augustine", Description = "히포의 주교. 서방 교회의 교부이자 위대한 신학자.", FeastDay = new DateTime(2000, 8, 28), Patronage = "신학자, 인쇄업자, 맥주 양조자", CreatedBy = "system" },
            new() { KoreanName = "성 김대건 안드레아", LatinName = "Sanctus Andreas Kim Taegon", EnglishName = "Saint Andrew Kim Taegon", Description = "한국 최초의 사제이자 순교자. 103위 한국 순교 성인의 대표.", FeastDay = new DateTime(2000, 9, 20), Patronage = "한국 성직자", CreatedBy = "system" },
            new() { KoreanName = "성 정하상 바오로", LatinName = "Sanctus Paulus Chong Hasang", EnglishName = "Saint Paul Chung Ha-sang", Description = "한국 103위 순교 성인 중 한 분. 평신도 지도자이자 순교자.", FeastDay = new DateTime(2000, 9, 20), Patronage = "한국 평신도", CreatedBy = "system" },
            new() { KoreanName = "성녀 모니카", LatinName = "Sancta Monica", EnglishName = "Saint Monica", Description = "성 아우구스티노의 어머니. 아들의 회심을 위해 기도한 어머니의 모범.", FeastDay = new DateTime(2000, 8, 27), Patronage = "어머니, 기혼 여성", CreatedBy = "system" },
            new() { KoreanName = "성 미카엘 대천사", LatinName = "Sanctus Michael Archangelus", EnglishName = "Saint Michael the Archangel", Description = "대천사. 악마와 싸워 이긴 하느님의 군대 지휘관.", FeastDay = new DateTime(2000, 9, 29), Patronage = "군인, 경찰, 소방관", CreatedBy = "system" },
            new() { KoreanName = "성 가브리엘 대천사", LatinName = "Sanctus Gabriel Archangelus", EnglishName = "Saint Gabriel the Archangel", Description = "대천사. 성모 마리아에게 예수 탄생을 알린 하느님의 메신저.", FeastDay = new DateTime(2000, 9, 29), Patronage = "통신업자, 우편배달부, 외교관", CreatedBy = "system" },
            new() { KoreanName = "성 라파엘 대천사", LatinName = "Sanctus Raphael Archangelus", EnglishName = "Saint Raphael the Archangel", Description = "대천사. 토비아를 보호하고 치유한 하느님의 치유자.", FeastDay = new DateTime(2000, 9, 29), Patronage = "여행자, 의사, 약사", CreatedBy = "system" },
            new() { KoreanName = "성 이냐시오 (로욜라)", LatinName = "Sanctus Ignatius de Loyola", EnglishName = "Saint Ignatius of Loyola", Description = "예수회 창설자. '영신수련'의 저자.", FeastDay = new DateTime(2000, 7, 31), Patronage = "예수회, 군인, 교육자", CreatedBy = "system" },
            new() { KoreanName = "성 프란치스코 하비에르", LatinName = "Sanctus Franciscus Xaverius", EnglishName = "Saint Francis Xavier", Description = "예수회 공동 창설자. 동아시아 선교의 개척자.", FeastDay = new DateTime(2000, 12, 3), Patronage = "선교사, 동아시아", CreatedBy = "system" },
            new() { KoreanName = "성녀 클라라", LatinName = "Sancta Clara Assisiensis", EnglishName = "Saint Clare of Assisi", Description = "클라라회 창설자. 프란치스코의 영적 동반자.", FeastDay = new DateTime(2000, 8, 11), Patronage = "텔레비전, 눈병 환자", CreatedBy = "system" },
            new() { KoreanName = "성 도미니코", LatinName = "Sanctus Dominicus", EnglishName = "Saint Dominic", Description = "도미니코회 창설자. 로사리오 기도의 전파자.", FeastDay = new DateTime(2000, 8, 8), Patronage = "천문학자, 도미니카 공화국", CreatedBy = "system" },
            new() { KoreanName = "성 요한 세례자", LatinName = "Sanctus Ioannes Baptista", EnglishName = "Saint John the Baptist", Description = "예수의 길을 예비한 예언자. 요르단 강에서 예수에게 세례를 베풂.", FeastDay = new DateTime(2000, 6, 24), Patronage = "세례, 요르단", CreatedBy = "system" },
            new() { KoreanName = "성 요한 사도", LatinName = "Sanctus Ioannes Apostolus", EnglishName = "Saint John the Apostle", Description = "예수가 사랑한 제자. 요한 복음서와 묵시록의 저자.", FeastDay = new DateTime(2000, 12, 27), Patronage = "작가, 출판업자", CreatedBy = "system" },
            new() { KoreanName = "성 루카", LatinName = "Sanctus Lucas", EnglishName = "Saint Luke", Description = "복음사가이자 의사. 루카 복음서와 사도행전의 저자.", FeastDay = new DateTime(2000, 10, 18), Patronage = "의사, 화가, 외과의사", CreatedBy = "system" },
            new() { KoreanName = "성 마르코", LatinName = "Sanctus Marcus", EnglishName = "Saint Mark", Description = "복음사가. 마르코 복음서의 저자이자 베네치아의 수호성인.", FeastDay = new DateTime(2000, 4, 25), Patronage = "베네치아, 변호사, 공증인", CreatedBy = "system" },
            new() { KoreanName = "성 마태오", LatinName = "Sanctus Matthaeus", EnglishName = "Saint Matthew", Description = "예수의 열두 사도 중 한 명. 세리에서 사도가 된 복음사가.", FeastDay = new DateTime(2000, 9, 21), Patronage = "세무사, 회계사, 은행원", CreatedBy = "system" },
            new() { KoreanName = "성녀 체칠리아", LatinName = "Sancta Caecilia", EnglishName = "Saint Cecilia", Description = "초대 교회의 순교 성녀. 음악의 수호성녀.", FeastDay = new DateTime(2000, 11, 22), Patronage = "음악가, 가수, 시인", CreatedBy = "system" },
            new() { KoreanName = "성녀 아녜스", LatinName = "Sancta Agnes", EnglishName = "Saint Agnes", Description = "로마의 어린 순교 성녀. 순결과 신앙의 상징.", FeastDay = new DateTime(2000, 1, 21), Patronage = "소녀, 순결, 약혼자", CreatedBy = "system" },
            new() { KoreanName = "성 세바스티아노", LatinName = "Sanctus Sebastianus", EnglishName = "Saint Sebastian", Description = "로마 군인이자 순교자. 화살에 맞아도 살아남은 것으로 유명.", FeastDay = new DateTime(2000, 1, 20), Patronage = "운동선수, 군인, 전염병 환자", CreatedBy = "system" },
            new() { KoreanName = "성 조르지오", LatinName = "Sanctus Georgius", EnglishName = "Saint George", Description = "용을 물리친 전설의 기사 성인. 많은 나라의 수호성인.", FeastDay = new DateTime(2000, 4, 23), Patronage = "군인, 기사, 영국", CreatedBy = "system" },
            new() { KoreanName = "성 니콜라오", LatinName = "Sanctus Nicolaus", EnglishName = "Saint Nicholas", Description = "미라의 주교. 산타클로스의 원형이 된 관대한 성인.", FeastDay = new DateTime(2000, 12, 6), Patronage = "어린이, 선원, 학생", CreatedBy = "system" },
            new() { KoreanName = "성녀 루치아", LatinName = "Sancta Lucia", EnglishName = "Saint Lucy", Description = "시라쿠사의 순교 성녀. 눈의 수호성녀.", FeastDay = new DateTime(2000, 12, 13), Patronage = "시각 장애인, 눈병 환자", CreatedBy = "system" },
            new() { KoreanName = "성 빈첸시오 드 폴", LatinName = "Sanctus Vincentius a Paulo", EnglishName = "Saint Vincent de Paul", Description = "빈첸시오 사랑의 딸회와 빈첸시오회 창설자. 자선의 아버지.", FeastDay = new DateTime(2000, 9, 27), Patronage = "자선 단체, 가난한 이", CreatedBy = "system" },
            new() { KoreanName = "성 보나벤투라", LatinName = "Sanctus Bonaventura", EnglishName = "Saint Bonaventure", Description = "프란치스코회 신학자이자 교회 학자. '천사적 박사'라 불림.", FeastDay = new DateTime(2000, 7, 15), Patronage = "신학자, 프란치스코회", CreatedBy = "system" },
            new() { KoreanName = "성 알베르토 대제", LatinName = "Sanctus Albertus Magnus", EnglishName = "Saint Albert the Great", Description = "도미니코회 주교이자 교회 학자. 토마스 아퀴나스의 스승.", FeastDay = new DateTime(2000, 11, 15), Patronage = "과학자, 자연과학", CreatedBy = "system" },
            new() { KoreanName = "성 막시밀리아노 콜베", LatinName = "Sanctus Maximilianus Kolbe", EnglishName = "Saint Maximilian Kolbe", Description = "아우슈비츠에서 다른 수감자를 대신해 죽음을 택한 순교자.", FeastDay = new DateTime(2000, 8, 14), Patronage = "마약 중독자, 언론인, 수감자", CreatedBy = "system" },
            new() { KoreanName = "성녀 파티마의 성모", LatinName = "Nostra Domina de Fatima", EnglishName = "Our Lady of Fatima", Description = "1917년 포르투갈 파티마에서 세 어린이에게 발현한 성모 마리아.", FeastDay = new DateTime(2000, 5, 13), Patronage = "포르투갈, 평화", CreatedBy = "system" },
            new() { KoreanName = "성 요한 보스코", LatinName = "Sanctus Ioannes Bosco", EnglishName = "Saint John Bosco", Description = "살레시오회 창설자. 청소년 교육에 헌신한 성인.", FeastDay = new DateTime(2000, 1, 31), Patronage = "청소년, 학생, 교육자", CreatedBy = "system" },
            new() { KoreanName = "성 비오 신부", LatinName = "Sanctus Pius Petrelcinensis", EnglishName = "Saint Padre Pio", Description = "오상의 은사를 받은 카푸친 수도사. 20세기 위대한 영성가.", FeastDay = new DateTime(2000, 9, 23), Patronage = "청소년 자원봉사자, 고해 사제", CreatedBy = "system" },
            new() { KoreanName = "성녀 리타", LatinName = "Sancta Rita Casciensis", EnglishName = "Saint Rita of Cascia", Description = "불가능한 일의 수호성녀. 이마에 가시의 상처를 받은 성녀.", FeastDay = new DateTime(2000, 5, 22), Patronage = "불가능한 일, 상처 입은 이", CreatedBy = "system" },
            new() { KoreanName = "성 유다 타대오", LatinName = "Sanctus Iudas Thaddaeus", EnglishName = "Saint Jude Thaddeus", Description = "예수의 열두 사도 중 한 분. 절망적인 상황의 수호성인.", FeastDay = new DateTime(2000, 10, 28), Patronage = "절망적인 상황, 병원", CreatedBy = "system" },
            new() { KoreanName = "성 요한 마리아 비안네", LatinName = "Sanctus Ioannes Maria Vianney", EnglishName = "Saint John Vianney", Description = "아르스의 본당 사제. 고해성사에 헌신한 사목의 모범.", FeastDay = new DateTime(2000, 8, 4), Patronage = "본당 사제, 고해 사제", CreatedBy = "system" },
            new() { KoreanName = "성 베네딕토", LatinName = "Sanctus Benedictus", EnglishName = "Saint Benedict", Description = "서방 수도원 제도의 아버지. 베네딕도 규칙서의 저자.", FeastDay = new DateTime(2000, 7, 11), Patronage = "유럽, 학생, 수도자", CreatedBy = "system" },
            new() { KoreanName = "성 십자가의 요한", LatinName = "Sanctus Ioannes a Cruce", EnglishName = "Saint John of the Cross", Description = "가르멜 수도회 개혁자이자 교회 학자. 신비 신학의 대가.", FeastDay = new DateTime(2000, 12, 14), Patronage = "신비가, 시인, 관상 수도자", CreatedBy = "system" },
            new() { KoreanName = "성녀 엘리사벳 (헝가리)", LatinName = "Sancta Elisabeth Hungariae", EnglishName = "Saint Elizabeth of Hungary", Description = "헝가리의 공주이자 성녀. 가난한 이들을 위해 헌신.", FeastDay = new DateTime(2000, 11, 17), Patronage = "자선, 빵 굽는 이, 과부", CreatedBy = "system" },
            new() { KoreanName = "성 토마스 모어", LatinName = "Sanctus Thomas Morus", EnglishName = "Saint Thomas More", Description = "영국의 대법관. 신앙을 지키기 위해 순교한 정치인.", FeastDay = new DateTime(2000, 6, 22), Patronage = "정치인, 변호사, 공무원", CreatedBy = "system" },
            new() { KoreanName = "성녀 요안나 다르크", LatinName = "Sancta Ioanna Arcensis", EnglishName = "Saint Joan of Arc", Description = "프랑스를 구한 소녀 전사. 하느님의 음성을 따른 순교 성녀.", FeastDay = new DateTime(2000, 5, 30), Patronage = "프랑스, 군인, 순교자", CreatedBy = "system" },
            new() { KoreanName = "성녀 마더 데레사", LatinName = "Sancta Teresia Calcuttensis", EnglishName = "Saint Mother Teresa", Description = "사랑의 선교회 창설자. 인도 콜카타의 가장 가난한 이들을 섬김.", FeastDay = new DateTime(2000, 9, 5), Patronage = "자원봉사자, 가난한 이", CreatedBy = "system" },
            new() { KoreanName = "성 이시도로 (농부)", LatinName = "Sanctus Isidorus Agricola", EnglishName = "Saint Isidore the Farmer", Description = "마드리드의 농부 성인. 소박한 신앙생활의 모범.", FeastDay = new DateTime(2000, 5, 15), Patronage = "농부, 노동자, 마드리드", CreatedBy = "system" },
            new() { KoreanName = "성 마르틴 드 포레스", LatinName = "Sanctus Martinus de Porres", EnglishName = "Saint Martin de Porres", Description = "페루 리마의 도미니코회 수사. 인종 차별을 극복한 겸손한 성인.", FeastDay = new DateTime(2000, 11, 3), Patronage = "혼혈인, 이발사, 사회정의", CreatedBy = "system" },
        };
    }
}

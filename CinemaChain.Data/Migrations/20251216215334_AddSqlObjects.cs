using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaChain.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSqlObjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Views
            migrationBuilder.Sql(@"CREATE OR REPLACE VIEW ""View_SessionDetails"" AS SELECT s.""SessionId"", m.""Title"" AS ""MovieTitle"", h.""Name"" AS ""HallName"", s.""StartTime"", s.""Price"" FROM ""Session"" s JOIN ""Movie"" m ON s.""MovieId"" = m.""MovieId"" JOIN ""Hall"" h ON s.""HallId"" = h.""HallId"";");
            migrationBuilder.Sql(@"CREATE OR REPLACE VIEW ""View_MovieStats"" AS SELECT m.""Title"", COUNT(s.""SessionId"") as ""SessionCount"" FROM ""Movie"" m LEFT JOIN ""Session"" s ON m.""MovieId"" = s.""MovieId"" GROUP BY m.""MovieId"", m.""Title"";");

            // Functions
            migrationBuilder.Sql(@"CREATE OR REPLACE FUNCTION ""GetFreeSeatsCount""(target_session_id INT) RETURNS INT AS $$ DECLARE total_seats INT; sold_tickets INT; BEGIN SELECT h.""SeatsCount"" INTO total_seats FROM ""Session"" s JOIN ""Hall"" h ON s.""HallId"" = h.""HallId"" WHERE s.""SessionId"" = target_session_id; SELECT COUNT(*) INTO sold_tickets FROM ""Ticket"" WHERE ""SessionId"" = target_session_id AND ""IsSold"" = TRUE; RETURN COALESCE(total_seats, 0) - COALESCE(sold_tickets, 0); END; $$ LANGUAGE plpgsql;");
            migrationBuilder.Sql(@"CREATE OR REPLACE FUNCTION ""GetCinemaRevenue""(cinema_id INT) RETURNS DECIMAL AS $$ DECLARE total_revenue DECIMAL; BEGIN SELECT COALESCE(SUM(s.""Price""), 0) INTO total_revenue FROM ""Ticket"" t JOIN ""Session"" s ON t.""SessionId"" = s.""SessionId"" JOIN ""Hall"" h ON s.""HallId"" = h.""HallId"" WHERE h.""CinemaId"" = cinema_id AND t.""IsSold"" = TRUE; RETURN total_revenue; END; $$ LANGUAGE plpgsql;");

            // Procedure
            migrationBuilder.Sql(@"CREATE OR REPLACE PROCEDURE ""sp_UpdateHallSeats""(target_hall_id INT, new_count INT) LANGUAGE plpgsql AS $$ BEGIN IF EXISTS (SELECT 1 FROM ""Session"" WHERE ""HallId"" = target_hall_id AND ""StartTime"" > NOW()) THEN RAISE EXCEPTION 'Cannot change seat count: Hall has future sessions.'; END IF; UPDATE ""Hall"" SET ""SeatsCount"" = new_count WHERE ""HallId"" = target_hall_id; END; $$;");

            // Triggers
            migrationBuilder.Sql(@"CREATE OR REPLACE FUNCTION ""CheckDoubleBooking""() RETURNS TRIGGER AS $$ BEGIN IF EXISTS (SELECT 1 FROM ""Ticket"" WHERE ""SessionId"" = NEW.""SessionId"" AND ""Row"" = NEW.""Row"" AND ""SeatNumber"" = NEW.""SeatNumber"" AND ""IsSold"" = TRUE) THEN RAISE EXCEPTION 'Seat % in Row % is already sold!', NEW.""SeatNumber"", NEW.""Row""; END IF; RETURN NEW; END; $$ LANGUAGE plpgsql; CREATE TRIGGER ""Trg_PreventDoubleBooking"" BEFORE INSERT ON ""Ticket"" FOR EACH ROW EXECUTE FUNCTION ""CheckDoubleBooking""();");
            migrationBuilder.Sql(@"CREATE OR REPLACE FUNCTION ""CheckTicketDate""() RETURNS TRIGGER AS $$ DECLARE session_start TIMESTAMP; BEGIN SELECT ""StartTime"" INTO session_start FROM ""Session"" WHERE ""SessionId"" = NEW.""SessionId""; IF session_start < NOW() THEN RAISE EXCEPTION 'Cannot sell ticket: Session has already started or finished.'; END IF; RETURN NEW; END; $$ LANGUAGE plpgsql; CREATE TRIGGER ""Trg_PreventPastTicketSale"" BEFORE INSERT ON ""Ticket"" FOR EACH ROW EXECUTE FUNCTION ""CheckTicketDate""();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}

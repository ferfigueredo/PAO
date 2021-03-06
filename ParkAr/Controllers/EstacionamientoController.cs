﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ParkAr.Models;
using System.Data.Entity;
using ParkAr.ViewModels;
using mercadopago;
using System.Collections;
using System.Web.Helpers;

namespace ParkAr.Controllers
{
    public class EstacionamientoController : Controller
    {

        private ApplicationDbContext _context;

        public EstacionamientoController()
        {
            _context = new ApplicationDbContext();
        }

        protected override void Dispose(bool disposing)
        {
            _context.Dispose();
        }


        public ActionResult Index()
        {
            /** Esto es para simplificar el desarrollo y no tener que estara logeandose a cada rato */
            var user = _context.Cientes.Include(x => x.Vehiculos).SingleOrDefault(p => p.Email == "ferfigueredo@gmail.com");

            //var user = _context.Cientes.SingleOrDefault(p => p.Email == "ferfigueredo@gmail.com");
            if (user != null && user.Vehiculos != null)
            {
                user.Vehiculos = user?.Vehiculos.Where(x => !x.Eliminado).ToList();
            }
            Cliente cliente = (Cliente)user;
            Session["user"] = cliente;


            var estacionamientos = _context.Estacionamientos.Include(e => e.Boxes).ToList();
            var model = new ReservaBoxViewModel
            {
                Cliente = (Cliente)Session["user"],
                Estacionamientos = estacionamientos

            };          


            return View(model);
        }

        public ActionResult VerBoxes(string estacionamientoID)
        {
            //var estacionamientos = _context.Estacionamientos.Include("Boxes").ToList();
            int id = Int32.Parse(estacionamientoID);
            var estacionamiento = _context.Estacionamientos
                                    .Include(x => x.Boxes.Select(y => y.EstadoBox)).SingleOrDefault(x => x.EstacionamientoId == id);

            /* **** Para agregar Boxes a un estacionamiento ***** */
            /*
           EstadoBox estadoBoxLibre = _context.EstadosBox.SingleOrDefault(y => y.EstadoBoxId == 1);
            CategoriaBox categoria = _context.CategoriasBox.SingleOrDefault(y => y.CategoriaBoxId == 1);
            int i = 1;
            while (i < 41) {
                Box box = new Box()
                {
                    Piso = 1,
                    Numero = i,
                    EstacionamientoId = estacionamiento.EstacionamientoId,
                    Estacionamiento = estacionamiento,
                    EstadoBox = estadoBoxLibre,
                    EstadoBoxId = estadoBoxLibre.EstadoBoxId,
                    CategoriaBox = categoria,
                    CategoriaBoxId = categoria.CategoriaBoxId
                    
                };
                i++;
                _context.Boxes.Add(box);
            }
            _context.SaveChanges();*/

            String nombre = estacionamiento.Nombre;
            VerBoxesViewModel model = new VerBoxesViewModel()
            {
                MapaBoxes = new Dictionary<int, ICollection<Box>>(),
                NombreEstacionamiento = nombre
            };
            foreach (Box box in estacionamiento.Boxes)
            {
                if (model.MapaBoxes.ContainsKey(box.Piso))
                {
                    ICollection<Box> boxesPiso = model.MapaBoxes[box.Piso];
                    boxesPiso.Add(box);
                }
                else
                {                    
                    ICollection<Box> boxesPiso = new List<Box>();
                    model.MapaBoxes.Add(box.Piso, boxesPiso);
                    boxesPiso.Add(box);
                }
            }
            
           

            return PartialView(model);
        }

        public ActionResult ReservarBox(string boxId, string estacionamientoId)
        {
            int id = Int32.Parse(boxId);
            int estacionamientoIdSeleccionado = Int32.Parse(estacionamientoId);
            var box = _context.Boxes.SingleOrDefault(x => x.BoxId == id);
            List < string > horas = new List<string>(new string[] { "1", "2", "3" });

            var estacionamientoSeleccionado = _context.Estacionamientos.
                                Include(x => x.Boxes.Select(y => y.EstadoBox)).Include(x => x.Boxes.Select(y => y.CategoriaBox)).SingleOrDefault(x => x.EstacionamientoId == estacionamientoIdSeleccionado);

            var boxSeleccionado = _context.Boxes.Include(x => x.CategoriaBox).Include(x => x.EstadoBox).SingleOrDefault(x => x.BoxId == id);

            Cliente cliente = (Cliente)Session["user"];

            Vehiculo vehiculo = cliente.getVehiculoPrincipal();

            var estacionamientos = _context.Estacionamientos.ToList();
            var tipoVehiculos = _context.TipoVehiculos.ToList();

            var model = new ReservaBoxViewModel()
            {
                BoxSeleccionado = boxSeleccionado,
                Cliente = cliente,
                TipoVehiculos = tipoVehiculos,
                EstacionamientoSeleccionado = estacionamientoSeleccionado,
                Vehiculo = vehiculo ,
                Desde = DateTime.Now,
                Hasta = DateTime.Now

            };
           
            return View("ReservaBox", model);

        }

        [HttpPost]
        public ActionResult GenerarReserva(String boxID, String estacionamientoID, String desde, String hasta, String vehiculoSel, ControllerContext context)
        {
            // traigo las reservas que tengo de la base de Datos para luego comparar
            //var reservasDb = _context.Reservas.Include(x => x.Box).ToList();

            var tempReserva = new Reserva();

            EstadoBox estadoBoxReservado = _context.EstadosBox.SingleOrDefault(y => y.EstadoBoxId == 3);         

            int boxIdInt = Int32.Parse(boxID);
             Box boxSeleccionado = _context.Boxes.Include(x => x.CategoriaBox).Include(x => x.EstadoBox).SingleOrDefault(x => x.BoxId == boxIdInt);
             boxSeleccionado.EstadoBox = estadoBoxReservado;
            
            DateTime dtDesde = Convert.ToDateTime(desde);
            DateTime dtHasta = Convert.ToDateTime(hasta);            
           
            Cliente cliente = (Cliente)Session["user"];           

            cliente = _context.Cientes.Include(x => x.Vehiculos).SingleOrDefault(p => p.ClienteId == cliente.ClienteId);
            
            int vehiculoSelId = Int32.Parse(vehiculoSel);
            Vehiculo vehiculoSeleccionado = _context.Vehiculos.SingleOrDefault(x => x.VehiculoId == vehiculoSelId);
            
            //tempReserva.BoxId = reservaModel.Box.BoxId;
            var estadoReserva = _context.EstadoReservas.SingleOrDefault(x => x.EstadoReservaId == 1);            

            tempReserva.FechaDesde = dtDesde;
            tempReserva.FechaHasta = dtHasta;
            tempReserva.Vehiculo = vehiculoSeleccionado;
            tempReserva.Box = boxSeleccionado;
            tempReserva.EstadoReserva = estadoReserva;
            tempReserva.BoxId = boxSeleccionado.BoxId;

             _context.Reservas.Add(tempReserva);

             _context.SaveChanges();

            int estacionamientoIdInt = Int32.Parse(estacionamientoID);
            var estacionamiento = _context.Estacionamientos.SingleOrDefault(x => x.EstacionamientoId == estacionamientoIdInt);


            var model = new ConfirmacionReservaViewModel()
            {
                Box = boxSeleccionado,
                Cliente = cliente,
                TipoVehiculoSeleccionado = vehiculoSeleccionado.TipoVehiculo,
                EstacionamientoSeleccionado = estacionamiento,
                Vehiculo = vehiculoSeleccionado,
                Desde = dtDesde,
                Hasta = dtHasta,
                MPCheckoutLink = ""
            };
    
            MP mp = new MP("1289778604132689", "zmbZeSQHeC8Zmb4rFbIBWHAHwm4thyig");
            //mp.sandboxMode(true);
            String preferenceData = "{\"items\":" +
                                        "[{" +
                                            "\"title\":\"Parkin: "+ estacionamiento.Nombre + "\"," +
                                            "\"quantity\":1," +
                                            "\"currency_id\":\"ARS\"," +
                                            "\"unit_price\":10.0" +
                                        "}]," +
                                        "\"back_urls\": {" +
                                            "\"success\": \"http://localhost:49557/Estacionamiento\"," +
                                            "\"failure\": \"http://localhost:49557/Estacionamiento\"," +
                                            "\"pending\": \"http://localhost:49557/Estacionamiento\"" +
                                        "}" +
                                    "}";

            Hashtable preference = mp.createPreference(preferenceData);
            Hashtable data = (Hashtable)preference["response"];
            String sandboxInitPoint = (String)data["sandbox_init_point"];

            model.MPCheckoutLink = sandboxInitPoint;

            return View("ConfirmacionReserva", model);
        }
   


      

        public PartialViewResult GetEstacionamiento(int id /* drop down value */)
        {
            var model = _context.Estacionamientos.Find(id); // This is for example put your code to fetch record. 
            
            return PartialView("_Details", model);
        }

        public ViewResult PayEstacionamiento(int id /* drop down value */)
        { 
            String sandboxInitPoint = "";

            return View("PayEstacionamiento", sandboxInitPoint);
        }

        public ViewResult ConfirmarcionPago(String msgMP)
        {

            String msg = msgMP + "";


            return View("ConfirmacionPago", "Pago confirmado");
        }
    }

   

}